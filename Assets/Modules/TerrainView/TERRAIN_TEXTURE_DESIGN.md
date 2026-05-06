# Terrain Texture Generation — Design Document

## Goal

Generate a procedural terrain texture in `TerrainViewTextureSubSystem` (Priority 200). The texture is a single baked `Texture2D` applied to the terrain mesh material. No custom shader required — standard URP Lit/Unlit with `_MainTex`.

## Chosen Approach: Brush-based Gaussian Splatting (Nadaraya-Watson Kernel Regression)

Each vertex in `VertexGrid` acts as a "brush center". The brush paints its classified color onto the output texture with Gaussian falloff. Overlapping brushes blend naturally — transitions between terrain zones emerge automatically from geometry without explicit blending code.

### Mathematical Foundation

**Nadaraya-Watson kernel regression (1964):**

```
                 Σ  K(distance(pixel, vertex_i)) × color_i
finalColor = ─────────────────────────────────────────────────
                 Σ  K(distance(pixel, vertex_i))
```

`K` is a kernel function (Gaussian): `K(d) = exp(-d² / 2σ²)`

Division by the sum of weights (normalization) guarantees the result stays within the range of input colors.

**Splatting (object-order) vs Nadaraya-Watson (image-order):**

Both are mathematically equivalent, just different iteration order:
- **NW (image-order):** outer loop over pixels → find nearby vertices. Better when pixels << vertices.
- **Splatting (object-order):** outer loop over vertices → paint nearby pixels. Better when vertices << pixels. **This is our case.**

### Why This Works for Transitions

At a border between plains and mountain slope:
- Plain vertices splat green with Gaussian falloff
- Mountain vertices splat grey with Gaussian falloff
- In the overlap zone: green + grey blend proportionally to distance
- Result: organic gradient without any explicit transition logic

This is analogous to how `HeightSmoothing` (Gaussian blur) already works in the project, but applied to color instead of height.

---

## Algorithm

### Input
- `VertexGrid` — all vertices with positions (heights in `Position.y`), owner hex coords
- Hex entities with tags: `HexMountTag`, `HexBedhillTag`, `HexWaterTag`
- Config: texture resolution, brush radius, Gaussian sigma, hue jitter strength

### Output
- `Texture2D` (RGBA32) — single texture covering the entire terrain mesh

### Steps

```
1. BUILD hex type map:
   For each hex entity:
     - Has<HexMountTag>()     → Mountain
     - Has<HexBedhillTag>()   → Bedhill
     - Has<HexWaterTag>()     → Water
     - None of the above      → Plain
   
   Coastline detection (second pass):
     For each Plain hex:
       If any axial neighbor has HexWaterTag → reclassify as Coastline
     (Use AxialMath.NeighborDirs for coarse hex neighbors)

2. COMPUTE world bounds from VertexGrid:
   worldMin = AxialToWorld(MinCoord)
   worldMax = AxialToWorld(MaxCoord)

3. ALLOCATE accumulators:
   float3[] colorAccum = new[resolution × resolution]   // RGB
   float[]  weightAccum = new[resolution × resolution]  // normalization weights

4. SPLAT (runs on background thread):
   For each vertex coord in VertexGrid.Coords:
     a. vertex = vertexGrid.TryGet(coord)
     b. Skip if MeshIndex == -1 (ghost vertex)
     c. Compute slope from neighbors (see Slope Computation below)
     d. Determine hex type from hexTypeMap via vertex owner
     e. Classify → baseColor (see Classification Table below)
     f. Apply micro-variation via hash (see Hash Function below)
     g. BFS within brushRadius from this vertex coord:
        For each visited neighbor vertex within radius:
          - worldDistance = |neighbor.Position.xz - vertex.Position.xz|
          - weight = exp(-worldDistance² / (2σ²))
          - Convert neighbor world pos → pixel coords:
            px = (pos.x - worldMin.x) / (worldMax.x - worldMin.x) × resolution
            py = (pos.z - worldMin.z) / (worldMax.z - worldMin.z) × resolution
          - colorAccum[px, py] += baseColor × weight
          - weightAccum[px, py] += weight

5. NORMALIZE:
   For each pixel:
     if weightAccum[i] > 0:
       finalColor[i] = colorAccum[i] / weightAccum[i]
     else:
       finalColor[i] = fallback color

6. UPLOAD (must run on main thread):
   Texture2D tex = new(resolution, resolution, TextureFormat.RGBA32, false)
   tex.SetPixels32(finalColors)
   tex.Apply()
   Apply to mesh material
```

### Alternative: Pixel-Centric Approach (Nadaraya-Watson Direct)

If vertex-centric splatting is complex for pixel coverage mapping:

```
For each pixel (px, py):
  worldPos = pixelToWorld(px, py)
  axialCoord = vertexGrid.WorldToAxial(worldPos)
  
  BFS from axialCoord within brushRadius:
    For each visited vertex:
      distance = |vertex.Position.xz - worldPos.xz|
      weight = gaussian(distance, sigma)
      Classify vertex → color
      accumColor += color × weight
      accumWeight += weight
  
  finalColor = accumColor / accumWeight
```

Simpler logic but more iterations when resolution > vertex count.

---

## Classification Table

| Hex Type | Slope | Surface | Color (RGB approx) |
|----------|-------|---------|-------------------|
| Plain | Low (< threshold) | Soft pastel grassland | (0.55, 0.78, 0.45) |
| Mountain | Low (plateau top) | Yellow-green highland | (0.72, 0.75, 0.38) |
| Mountain / Bedhill | High (steep) | Rocky slope | (0.58, 0.55, 0.50) |
| Coastline (plain adj. to water) | Low | Sandy shore | (0.82, 0.76, 0.55) |
| Water | Any | Water surface | (0.28, 0.45, 0.62) |

Colors are starting points — to be tuned visually via config.

---

## Slope Computation

For vertex at coord `c`:

```
neighbors = vertexGrid.GetNeighbors(c, span6)
maxDelta = 0
for each neighbor n in neighbors:
  if vertexGrid.TryGet(n, out nv):
    delta = abs(vertex.Position.y - nv.Position.y)
    maxDelta = max(maxDelta, delta)
slope = maxDelta / vertexGrid.CellSize   // normalize by cell spacing
```

Slope threshold for "steep" vs "flat" should be a config parameter.

---

## Micro-Variation (No Noise)

Project constraint: no Perlin/simplex/any noise functions. Use deterministic spatial hash instead.

```
uint Hash(float2 p):
  uint x = asuint(p.x)
  uint y = asuint(p.y)
  uint h = x * 73856093 ^ y * 19349663
  h = (h ^ (h >> 16)) * 0x45d9f3b
  return h ^ (h >> 16)

float HashFloat01(float2 p):
  return Hash(p) / (float)uint.MaxValue   // range [0, 1]
```

Apply as hue shift: rotate base color hue by `(HashFloat01(vertex.xz) - 0.5) * hueJitterStrength`.

---

## Configurable Parameters

| Parameter | Default | Purpose |
|-----------|---------|---------|
| `enabled` | true | Enable/disable texture generation |
| `textureResolution` | 2048 | Output texture size (width = height) |
| `brushRadius` | 3 | BFS depth for vertex brush (vertex-grid steps) |
| `gaussianSigma` | 1.5 | Gaussian falloff sigma (larger = softer transitions) |
| `hueJitterStrength` | 0.05 | Per-vertex random hue variation magnitude |
| `slopeThreshold` | 0.5 | Slope value separating "flat" from "steep" |

---

## Falloff Function Options

| Function | Formula | Character |
|----------|---------|-----------|
| Gaussian | `exp(-d²/(2σ²))` | Soft center, natural falloff |
| Linear | `max(0, 1 - d/r)` | Uniform fade |
| Smoothstep | `smoothstep(r, 0, d)` | Plateau in center, soft edge |
| Cosine | `(1 + cos(π·d/r)) / 2` | Between linear and Gaussian |

Recommended start: **Gaussian** — most natural for terrain painting.

---

## Pipeline Integration

```
TerrainViewConfigLoaderSystem
  → Loads TerrainTextureConfig via IAddressable
  → Creates TerrainTextureConfigComponent entity

TerrainViewGenerationSubSystem (Priority 100)
  → Mesh generated, heights in VertexGrid

TerrainViewTextureSubSystem (Priority 200)
  → Reads VertexGrid + hex tags + config from ECS
  → Background thread: classify vertices + splat colors
  → Main thread: create Texture2D, apply to material

TerrainViewSystem (orchestrator)
  → ApplyHeightsFromVertexGrid
  → (texture already applied by subsystem)
  → Create TerrainViewComponent entity
```

---

## Architecture Notes

- Brush abstraction `(vertex) → (color, radius, falloff)` allows future swap to texture-based brushes without changing the splatting pipeline
- Splatting with normalization is order-independent (commutative) — can be parallelized
- Follows existing patterns: BFS neighbor iteration from `HeightSmoothing`, config loading from `TerrainViewConfigLoaderSystem`, DI from `TerrainViewGenerationSubSystem`
- All methods must be instance methods (project constraint)
- Heavy computation on background thread via `UniTask.RunOnThreadPool`; `Texture2D` creation/upload on main thread

---

## Future Extensions

- **Texture-based brushes:** replace `classify() → color` with `sampleTexture(vertex.uv) → color`, same pipeline
- **Ambient occlusion bake:** darken vertices where neighbors are higher (valley darkening)
- **Edge highlighting:** darken along steep height transitions (isoline emphasis)
- **Per-hex hue variation:** `hash(hexCoord)` shifts hue per hex for variety within same type
- **Adaptive brush radius:** larger on plains, smaller on steep slopes for detail preservation
- **Multi-pass painting:** first pass = base color, second pass = detail overlay
