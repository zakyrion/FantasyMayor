# Terrain Texture Generation — Design Document

## Goal

Generate a procedural terrain texture in `TerrainViewTextureSubSystem` (Priority 200). The texture is a single baked `Texture2D` applied to the terrain mesh material. No custom shader required — standard URP Lit/Unlit with `_MainTex`.

---

## Implementation Status (2026-05-11)

### What Is Done

**Infrastructure (working):**
- `TerrainTextureConfig.cs` — ScriptableObject with colors, brush, slope, resolution
- `TerrainTextureConfigComponent.cs` — flattened ECS struct + `FromConfig()`
- `TerrainTextureComponent.cs` — transient ECS component carrying generated `Texture2D`
- `TerrainViewConfigLoaderSystem.cs` — loads `TerrainTextureConfig` via `IAddressable` (address: `"TerrainTextureConfig"`)
- `TerrainTextureConfig.asset` — lives in `Assets/Addressables/Configs/TerrainViewConfigs/`, registered in Terrain addressable group
- `TerrainViewInstaller` — already registers `TerrainViewTextureSubSystem` as `ViewSubSystem`
- `TerrainView.ApplyTexture(Texture2D)` — sets `material.mainTexture`, registers `Object.Destroy` in `AddDisposable`
- `TerrainViewSystem.ApplyGeneratedTexture()` — after `RunViewSubSystemsAsync`, reads `TerrainTextureComponent`, applies to TerrainView, destroys entity

**Pipeline flow (working):**
```
TerrainViewConfigLoaderSystem → loads TerrainTextureConfig, creates TerrainTextureConfigComponent entity
TerrainViewSystem.LoadAndSetupAsync:
  1. Load TerrainView prefab
  2. terrainView.Generate(hexCoords) — mesh + UVs
  3. RunViewSubSystemsAsync:
     - TerrainViewGenerationSubSystem (Priority 100) — isolines, erosion → heights in VertexGrid
     - TerrainViewTextureSubSystem (Priority 200) — classify + splat → Texture2D → TerrainTextureComponent entity
  4. ApplyGeneratedTexture(terrainView) — reads TerrainTextureComponent, applies texture
  5. ApplyHeightsFromVertexGrid — final mesh heights
  6. Create TerrainViewComponent entity
```

**Splatting algorithm (implemented, NOT working correctly):**
- Hex type map: HexMountTag → Mountain, HexBedhillTag → Bedhill, HexWaterTag → Water, else Plain; second pass detects Coastline (plain adjacent to water)
- For each VertexGrid vertex (skip OwnerCount == 0): classify by owner hex type + slope → pick color → hue jitter → BFS splat to nearby pixels with linear weight falloff
- Normalize accumulated colors → Color32[] → Texture2D on main thread

### What Is Broken — UV / Coordinate Mismatch

**The core problem:** the mesh UV space and the texture pixel space use different coordinate systems.

**Mesh UVs** (in `TerrainView.GenerateMesh()`):
- Positions from `AxialMath.AxialToWorld2D(hex, hexSize)` — **pointy-top** orientation
- Corners: `center + hexSize * (cos(60°i + 30°), sin(60°i + 30°))`
- Subdivided via barycentric interpolation
- Bounds computed from ALL mesh vertex positions (Vector2 XY where X=worldX, Y=worldZ)
- UV: `((pos.x - boundsMin.x) / boundsSize.x, (pos.y - boundsMin.y) / boundsSize.y)`

**Texture pixels** (in `TerrainViewTextureSubSystem`):
- Vertex positions from `VertexGrid` — uses **flat-top** axial orientation internally
- `VertexGrid.AxialToWorld(coord)` uses `AxialToWorldFlatTop(coord, vertexCellSize)`
- Bounds: attempted hex-geometry-based (pointy-top corners) but vertex world positions come from flat-top grid
- Pixel: `px = (vertex.Position.x - worldMin.x) / worldSize.x * (res-1)`

**Why they don't match:**
1. The mesh vertex positions and VertexGrid vertex positions are in the same world XZ space, but at DIFFERENT positions. The mesh uses barycentric subdivision of hex triangles; the VertexGrid uses BFS expansion on a flat-top axial grid. They are not 1:1.
2. `ApplyHeightsFromVertexGrid` bridges this gap by doing `vertexGrid.WorldToAxial(meshVertex.xz)` lookup — but for texture generation we go the other direction (VertexGrid positions → texture pixels), and those positions don't correspond to mesh UV positions.
3. VertexGrid also includes ghost water edge padding hexes that extend beyond the mesh boundary.

**Problem 2: Sparse pixel coverage (holes in texture).**
The splatting writes ONLY to pixels that correspond to VertexGrid vertex positions. At 2048×2048 = ~4M pixels vs ~10-30k vertices, the vast majority of pixels receive zero weight and get the fallback color. Between splatted pixels there are gaps ("holes") that appear as fallback-colored dots or patches. Bilinear filtering smooths this at the GPU level during rendering, but only if splatted pixels are dense enough — with large resolution-to-vertex ratios the holes dominate.

**Possible fixes to explore:**
- **Option A (pixel-centric):** Iterate ALL texture pixels, convert pixel→world→axial, BFS from nearest vertex, accumulate colors. Every pixel gets a value — no holes. Slower (4M iterations), but simple and guaranteed full coverage. Can be optimized by lowering resolution or caching BFS results.
- **Option B (mesh vertex iteration):** Iterate MESH vertices (not VertexGrid). Read vertex positions + UVs from the mesh, look up hex type from VertexGrid via `WorldToAxial`, splat at UV-derived pixel positions. Guarantees UV alignment, but still has the hole problem (mesh vertices are also sparse relative to 2048²).
- **Option C (lower resolution):** Match texture resolution to vertex density. If ~30k vertices, a 256×256 or 512×512 texture with bilinear filtering may look fine without holes.
- **Option D (post-process fill):** After splatting, flood-fill or dilate pixels with zero weight from their nearest non-zero neighbors. Covers holes but adds a pass.
- **Option E (pixel-radius splat):** Instead of BFS over vertex neighbors, for each vertex splat to ALL pixels within a world-space radius. Converts the problem to pixel-space coverage. Guarantees full coverage if radius is large enough relative to vertex spacing.

### Bugs Found and Fixed
- `HexVertex.MeshIndex` is NEVER set anywhere in the codebase (always -1). Filtering `MeshIndex == -1` skipped ALL vertices → solid fallback color. Fixed: use `OwnerCount == 0` to detect ghost vertices.

---

## Files

| File | Purpose |
|------|---------|
| `Configs/TerrainTextureConfig.cs` | ScriptableObject: resolution, brush, slope, colors |
| `Components/TerrainTextureConfigComponent.cs` | Flattened ECS struct + `FromConfig()` |
| `Components/TerrainTextureComponent.cs` | Transient Texture2D carrier between subsystem and orchestrator |
| `Systems/TerrainViewTextureSubSystem.cs` | Main splatting logic (needs UV fix) |
| `Systems/TerrainViewConfigLoaderSystem.cs` | Loads TerrainTextureConfig (modified) |
| `Systems/TerrainViewSystem.cs` | Applies texture after subsystems (modified) |
| `Views/TerrainView.cs` | `ApplyTexture()` method (modified) |
| `Addressables/.../TerrainTextureConfig.asset` | Config asset with default colors |

---

## Algorithm (Current — Linear Falloff BFS Splatting)

Simplified from original NW kernel regression to linear BFS wave falloff:

```
For each vertex V in VertexGrid (skip OwnerCount == 0):
  hexType = hexTypeMap[V.owner[0]]
  slope = max(|V.y - neighbor.y|) / vertexGrid.CellSize
  color = PickColor(hexType, slope, slopeThreshold)
  color = ApplyHueJitter(color, V.Position.xz)
  
  BFS from V, depth 0..brushRadius:
    For each visited vertex N at depth d:
      weight = lerp(centerWeight, edgeWeight, d / brushRadius)
      pixel = worldToPixel(N.Position.xz)
      colorAccum[pixel] += color * weight
      weightAccum[pixel] += weight

Normalize: pixel[i] = colorAccum[i] / weightAccum[i]  (or fallback if weight == 0)
```

---

## Classification Table

| Hex Type | Slope | Color field |
|----------|-------|-------------|
| Plain | any | `plainColor` |
| Mountain | ≤ threshold | `mountainFlatColor` |
| Mountain | > threshold | `mountainSteepColor` |
| Bedhill | ≤ threshold | `bedhillColor` |
| Bedhill | > threshold | `mountainSteepColor` |
| Coastline | any | `coastlineColor` |
| Water | any | `waterColor` |

---

## Config Parameters

| Parameter | Default | Purpose |
|-----------|---------|---------|
| `textureResolution` | 2048 | Output texture size |
| `brushRadius` | 3 | BFS depth for splatting |
| `centerWeight` | 1.0 | Weight at BFS depth 0 |
| `edgeWeight` | 0.3 | Weight at BFS depth = brushRadius |
| `slopeThreshold` | 0.5 | Steep vs flat cutoff |
| `hueJitterStrength` | 0.05 | Per-vertex hue variation |
| Colors | see asset | Per-type base colors |

---

## Micro-Variation (No Noise — project constraint)

Deterministic spatial hash:
```
uint Hash(float2 p):
  uint x = asuint(p.x)
  uint y = asuint(p.y)
  uint h = x * 73856093 ^ y * 19349663
  h = (h ^ (h >> 16)) * 0x45d9f3b
  return h ^ (h >> 16)

float HashFloat01(float2 p):
  return Hash(p) / (float)uint.MaxValue
```
Hue shift: `(HashFloat01(vertex.xz) - 0.5) * hueJitterStrength`

---

## Key Coordinate Systems

| System | Orientation | Conversion | Used by |
|--------|-------------|------------|---------|
| Hex grid (coarse) | Pointy-top | `AxialToWorldPointTop(hex, hexSize)` | Mesh generation, hex entities |
| VertexGrid (fine) | Flat-top | `AxialToWorldFlatTop(coord, vertexCellSize)` | Height field, erosion, texture splatting |
| Mesh UVs | — | `(worldX - boundsMin.x) / boundsSize.x` | GPU texture sampling |

Both coordinate systems produce positions in the same world XZ space, but at different grid points. `VertexGrid.WorldToAxial()` bridges mesh→vertex lookup (used by `ApplyHeightsFromVertexGrid`).

---

## Future Extensions

- Texture-based brushes: replace `classify() → color` with `sampleTexture(vertex.uv) → color`
- Ambient occlusion bake: darken vertices where neighbors are higher
- Edge highlighting: darken along steep height transitions
- Per-hex hue variation: `hash(hexCoord)` shifts hue per hex
- Adaptive brush radius: larger on plains, smaller on steep slopes
- Multi-pass painting: first pass = base color, second pass = detail overlay
