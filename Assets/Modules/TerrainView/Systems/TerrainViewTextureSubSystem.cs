using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.HexesCore.Components;
using Modules.HexesCore.Data;
using Modules.HexesCore.Utils;
using Modules.TerrainView.Components;
using Unity.Mathematics;
using UnityEngine;

namespace Modules.TerrainView.Systems
{
    /// <summary>
    ///     Generates a procedural terrain texture by classifying VertexGrid vertices and
    ///     rasterizing their color regions into a 2D pixel buffer. Each seed vertex classifies
    ///     its owning hex (mountain, water, plain, coastline, bedhill), collects a BFS brush region,
    ///     triangulates the region using consecutive flat-top neighbor pairs, and rasterizes each
    ///     triangle directly into the output texture. A global visited set prevents any vertex from
    ///     contributing to more than one color region. Runs after
    ///     <see cref="TerrainViewGenerationSubSystem" /> (Priority 200).
    /// </summary>
    [UsedImplicitly]
    internal sealed class TerrainViewTextureSubSystem : ViewSubSystem
    {
        private const int ExecutionPriority = 200;

        /// <summary>Hex terrain classification for color lookup.</summary>
        private enum HexType : byte
        {
            Plain = 0,
            Mountain = 1,
            Bedhill = 2,
            Water = 3,
            Coastline = 4
        }

        private readonly EntitySet _hexSet;
        private readonly EntitySet _terrainConfigSet;
        private readonly EntitySet _textureConfigSet;
        private readonly EntitySet _vertexGridSet;
        private readonly World _world;

        /// <inheritdoc />
        public override int Priority => ExecutionPriority;

        /// <param name="world">ECS world used for entity queries and texture component creation.</param>
        public TerrainViewTextureSubSystem(World world)
        {
            _world = world;
            _hexSet = world.GetEntities().With<HexIdComponent>().AsSet();
            _terrainConfigSet = world.GetEntities().With<TerrainViewConfigComponent>().AsSet();
            _textureConfigSet = world.GetEntities().With<TerrainTextureConfigComponent>().AsSet();
            _vertexGridSet = world.GetEntities().With<VertexGridComponent>().AsSet();
        }

        /// <inheritdoc />
        public override async UniTask Update(GameState state, CancellationToken cancellationToken)
        {
            if (_textureConfigSet.Count == 0 || _vertexGridSet.Count == 0 || _terrainConfigSet.Count == 0)
            {
                Debug.LogError("[TerrainViewTextureSubSystem] Required config or vertex grid entity is missing.");
                return;
            }

            var config = _textureConfigSet.GetEntities()[0].Get<TerrainTextureConfigComponent>();
            var terrainConfig = _terrainConfigSet.GetEntities()[0].Get<TerrainViewConfigComponent>();
            var vertexGrid = _vertexGridSet.GetEntities()[0].Get<VertexGridComponent>().Grid;

            var hexTypeMap = BuildHexTypeMap();
            var hexSize = terrainConfig.CellSize;

            Color32[] pixels = null;
            await UniTask.RunOnThreadPool(
                () => { pixels = GeneratePixels(vertexGrid, hexTypeMap, config, hexSize); },
                cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            var resolution = config.TextureResolution;
            var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false);

            _world.CreateEntity().Set(new TerrainTextureComponent { Texture = texture });
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _hexSet.Dispose();
            _terrainConfigSet.Dispose();
            _textureConfigSet.Dispose();
            _vertexGridSet.Dispose();
            base.Dispose();
        }

        /// <summary>
        ///     Builds a lookup from <see cref="HexCoord" /> to <see cref="HexType" /> using ECS tags.
        ///     First pass classifies by tag; second pass detects coastline (plain hex adjacent to water).
        /// </summary>
        /// <returns>Dictionary mapping each hex coordinate to its terrain type.</returns>
        private Dictionary<HexCoord, HexType> BuildHexTypeMap()
        {
            var entities = _hexSet.GetEntities();
            var map = new Dictionary<HexCoord, HexType>(entities.Length);

            foreach (ref readonly var entity in entities)
            {
                var coord = entity.Get<HexIdComponent>().Coords;

                if (entity.Has<HexMountTag>())
                    map[coord] = HexType.Mountain;
                else if (entity.Has<HexBedhillTag>())
                    map[coord] = HexType.Bedhill;
                else if (entity.Has<HexWaterTag>())
                    map[coord] = HexType.Water;
                else
                    map[coord] = HexType.Plain;
            }

            foreach (ref readonly var entity in entities)
            {
                var coord = entity.Get<HexIdComponent>().Coords;
                if (map[coord] != HexType.Plain)
                    continue;

                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = coord + AxialMath.NeighborsPointyTop[d];
                    if (map.TryGetValue(neighbor, out var neighborType) && neighborType == HexType.Water)
                    {
                        map[coord] = HexType.Coastline;
                        break;
                    }
                }
            }

            return map;
        }

        /// <summary>
        ///     Generates the full pixel array on a background thread.
        ///     Iterates all non-ghost VertexGrid vertices in a single global pass. For each vertex,
        ///     classifies the owning hex and emits all flat-top triangles for which this vertex is
        ///     the canonical (lex-smallest by q then r) of the three corners. This guarantees each
        ///     triangle is rasterized exactly once with no seams between classification regions.
        ///     Triangles where any corner is a ghost vertex (no owner) are excluded; the thin fringe
        ///     at the terrain boundary falls back to <see cref="TerrainTextureConfigComponent.FallbackColor" />.
        /// </summary>
        /// <param name="vertexGrid">Vertex grid with positions and ownership data.</param>
        /// <param name="hexTypeMap">Pre-built hex classification lookup.</param>
        /// <param name="config">Flattened texture config from ECS.</param>
        /// <param name="hexSize">Hex cell radius in world units (pointy-top).</param>
        /// <returns>Pixel array ready for <see cref="Texture2D.SetPixels32" />.</returns>
        private Color32[] GeneratePixels(
            VertexGrid vertexGrid,
            Dictionary<HexCoord, HexType> hexTypeMap,
            TerrainTextureConfigComponent config,
            float hexSize)
        {
            var resolution = config.TextureResolution;

            ComputeSquareUVRect(hexTypeMap, hexSize, out var squareMin, out var squareSize);
            if (squareSize <= 0f)
                return BuildFallbackPixels(resolution * resolution, config.FallbackColor);

            var pixels = BuildFallbackPixels(resolution * resolution, config.FallbackColor);
            var slopeThreshold = config.SlopeThreshold;
            var hueJitter = config.HueJitterStrength;

            foreach (var coord in vertexGrid.Coords)
            {
                if (!vertexGrid.TryGet(coord, out var vertex))
                    continue;

                if (vertex.OwnerCount == 0)
                    continue;

                var slope = ComputeSlope(coord, vertex, vertexGrid, vertexGrid.CellSize);
                var baseColor = ComputeBlendedColor(vertex.Position.xz, vertex[0], slope, hexSize, slopeThreshold, config, hexTypeMap);

                if (hueJitter > 0f)
                    baseColor = ApplyHueJitter(baseColor, vertex.Position.xz, hueJitter);

                var c32 = new Color32(
                    (byte)(baseColor.r * 255f),
                    (byte)(baseColor.g * 255f),
                    (byte)(baseColor.b * 255f),
                    255);

                var p0 = WorldToPixel(vertex.Position.xz, squareMin, squareSize, resolution);

                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var ni = coord + AxialMath.NeighborsFlatTop[d];
                    var ni1 = coord + AxialMath.NeighborsFlatTop[(d + 1) % AxialMath.NeighborCount];

                    if (!vertexGrid.TryGet(ni, out var niVertex) || niVertex.OwnerCount == 0)
                        continue;

                    if (!vertexGrid.TryGet(ni1, out var ni1Vertex) || ni1Vertex.OwnerCount == 0)
                        continue;

                    if (!IsCanonical(coord, ni, ni1))
                        continue;

                    var p1 = WorldToPixel(niVertex.Position.xz, squareMin, squareSize, resolution);
                    var p2 = WorldToPixel(ni1Vertex.Position.xz, squareMin, squareSize, resolution);

                    RasterizeTriangle(p0, p1, p2, resolution, c32, pixels);
                }
            }

            return pixels;
        }

        /// <summary>
        ///     Computes a square UV rect from hex centers plus their six pointy-top corners,
        ///     using the same formula as <see cref="Views.TerrainView.ComputeSquareUVRect" />
        ///     so that VertexGrid vertex positions map to the identical UV space as the mesh.
        /// </summary>
        /// <param name="hexTypeMap">All hex coords (used as iteration source).</param>
        /// <param name="hexSize">Hex cell radius in world units.</param>
        /// <param name="squareMin">Output: lower-left corner of the square in world XZ.</param>
        /// <param name="squareSize">Output: side length of the square in world units (same for X and Z).</param>
        private void ComputeSquareUVRect(
            Dictionary<HexCoord, HexType> hexTypeMap,
            float hexSize,
            out float2 squareMin,
            out float squareSize)
        {
            var minX = float.MaxValue;
            var minZ = float.MaxValue;
            var maxX = float.MinValue;
            var maxZ = float.MinValue;

            foreach (var hexCoord in hexTypeMap.Keys)
            {
                var center = AxialMath.AxialToWorld2D(hexCoord.Value, hexSize);

                if (center.x < minX) minX = center.x;
                if (center.x > maxX) maxX = center.x;
                if (center.y < minZ) minZ = center.y;
                if (center.y > maxZ) maxZ = center.y;

                for (var i = 0; i < 6; i++)
                {
                    var angle = math.radians(60f * i + 30f);
                    var cx = (float)(center.x + hexSize * math.cos(angle));
                    var cz = (float)(center.y + hexSize * math.sin(angle));

                    if (cx < minX) minX = cx;
                    if (cx > maxX) maxX = cx;
                    if (cz < minZ) minZ = cz;
                    if (cz > maxZ) maxZ = cz;
                }
            }

            var sizeX = maxX - minX;
            var sizeZ = maxZ - minZ;
            squareSize = math.max(sizeX, sizeZ);

            var centerX = (minX + maxX) * 0.5f;
            var centerZ = (minZ + maxZ) * 0.5f;
            squareMin = new float2(centerX - squareSize * 0.5f, centerZ - squareSize * 0.5f);
        }

        /// <summary>
        ///     Computes the normalized slope at a vertex by examining the maximum height delta
        ///     among its axial neighbors, divided by the vertex grid cell size.
        /// </summary>
        /// <param name="coord">Axial coordinate of the vertex.</param>
        /// <param name="vertex">The vertex value.</param>
        /// <param name="vertexGrid">Source vertex grid.</param>
        /// <param name="cellSize">Vertex grid cell spacing for normalization.</param>
        /// <returns>Slope value (0 = flat, higher = steeper).</returns>
        private float ComputeSlope(
            VertexCoord coord,
            in HexVertex vertex,
            VertexGrid vertexGrid,
            float cellSize)
        {
            var maxDelta = 0f;

            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                var neighborCoord = coord + AxialMath.NeighborsFlatTop[d];
                if (!vertexGrid.TryGet(neighborCoord, out var neighborVertex))
                    continue;

                var delta = math.abs(vertex.Position.y - neighborVertex.Position.y);
                if (delta > maxDelta)
                    maxDelta = delta;
            }

            return cellSize > 0f ? maxDelta / cellSize : 0f;
        }

        /// <summary>
        ///     Computes the terrain color at <paramref name="worldXZ" /> by blending the colors of
        ///     the primary hex and its six pointy-top neighbors, weighted by linear distance falloff.
        ///     The falloff radius equals <c>hexSize × √3</c> — the center-to-center distance between
        ///     adjacent pointy-top hexes — so influence drops to zero exactly at the next hex center.
        ///     A vertex at a hex center receives purely that hex's color; a vertex on the shared edge
        ///     of two hexes receives a 50/50 blend; a vertex at a shared corner receives equal
        ///     contributions from all three meeting hexes.
        /// </summary>
        /// <param name="worldXZ">Vertex world XZ position.</param>
        /// <param name="primaryHex">The hex that owns this vertex.</param>
        /// <param name="slope">Pre-computed slope at this vertex for steep/flat color selection.</param>
        /// <param name="hexSize">Hex cell radius in world units (pointy-top).</param>
        /// <param name="slopeThreshold">Threshold above which the steep color variant applies.</param>
        /// <param name="config">Config holding all color definitions.</param>
        /// <param name="hexTypeMap">Hex classification lookup.</param>
        /// <returns>Blended terrain color at the given world position.</returns>
        private Color ComputeBlendedColor(
            float2 worldXZ,
            HexCoord primaryHex,
            float slope,
            float hexSize,
            float slopeThreshold,
            in TerrainTextureConfigComponent config,
            Dictionary<HexCoord, HexType> hexTypeMap)
        {
            var radius = hexSize * math.sqrt(3f);

            var r = 0f;
            var g = 0f;
            var b = 0f;
            var totalWeight = 0f;

            // i = -1 → primary hex; i = 0..5 → six pointy-top neighbors.
            for (var i = -1; i < AxialMath.NeighborCount; i++)
            {
                var hex = i < 0 ? primaryHex : primaryHex + AxialMath.NeighborsPointyTop[i];

                if (!hexTypeMap.TryGetValue(hex, out var hexType))
                    continue;

                var center = AxialMath.AxialToWorld2D(hex.Value, hexSize);
                var dist = math.length(worldXZ - center);
                var weight = math.max(0f, radius - dist);
                if (weight <= 0f)
                    continue;

                var c = PickColor(hexType, slope, slopeThreshold, config);
                r += c.r * weight;
                g += c.g * weight;
                b += c.b * weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0f)
            {
                var fallbackType = hexTypeMap.TryGetValue(primaryHex, out var ft) ? ft : HexType.Plain;
                return PickColor(fallbackType, slope, slopeThreshold, config);
            }

            return new Color(r / totalWeight, g / totalWeight, b / totalWeight);
        }

        /// <summary>
        ///     Selects a base color from the config based on hex type and slope.
        ///     Mountain and bedhill vertices on steep slopes use the steep color variant.
        /// </summary>
        /// <param name="hexType">Classified terrain type.</param>
        /// <param name="slope">Normalized slope at the vertex.</param>
        /// <param name="slopeThreshold">Threshold above which the steep variant applies.</param>
        /// <param name="config">Config holding all color definitions.</param>
        /// <returns>Base color before hue jitter.</returns>
        private Color PickColor(
            HexType hexType,
            float slope,
            float slopeThreshold,
            in TerrainTextureConfigComponent config)
        {
            return hexType switch
            {
                HexType.Mountain => slope > slopeThreshold ? config.MountainSteepColor : config.MountainFlatColor,
                HexType.Bedhill => slope > slopeThreshold ? config.MountainSteepColor : config.BedhillColor,
                HexType.Water => config.WaterColor,
                HexType.Coastline => config.CoastlineColor,
                _ => config.PlainColor
            };
        }

        /// <summary>
        ///     Applies a deterministic hue shift to the base color using a spatial hash.
        ///     No Perlin/simplex noise — project constraint.
        /// </summary>
        /// <param name="color">Input color to jitter.</param>
        /// <param name="worldXZ">Vertex world XZ position used as hash seed.</param>
        /// <param name="strength">Maximum hue shift magnitude.</param>
        /// <returns>Color with hue rotated by a deterministic amount.</returns>
        private Color ApplyHueJitter(Color color, float2 worldXZ, float strength)
        {
            var hash01 = HashFloat01(worldXZ);
            var shift = (hash01 - 0.5f) * strength;

            Color.RGBToHSV(color, out var h, out var s, out var v);
            h = (h + shift + 1f) % 1f;
            return Color.HSVToRGB(h, s, v);
        }

        /// <summary>
        ///     Deterministic spatial hash producing a float in [0, 1].
        ///     Uses large co-prime multipliers on the raw bit pattern of the input floats.
        /// </summary>
        /// <param name="p">World XZ position.</param>
        /// <returns>Pseudo-random value in [0, 1].</returns>
        private float HashFloat01(float2 p)
        {
            var x = math.asuint(p.x);
            var y = math.asuint(p.y);
            var h = x * 73856093u ^ y * 19349663u;
            h = (h ^ (h >> 16)) * 0x45d9f3bu;
            h ^= h >> 16;
            return h / (float)uint.MaxValue;
        }

        /// <summary>
        ///     Returns <c>true</c> when <paramref name="w" /> is the lexicographically smallest
        ///     vertex among the three by (q, r), ensuring each triangle is emitted exactly once
        ///     across all three vertex iterations.
        /// </summary>
        /// <param name="w">Candidate canonical vertex.</param>
        /// <param name="a">Second vertex of the triangle.</param>
        /// <param name="b">Third vertex of the triangle.</param>
        /// <returns>True if <paramref name="w" /> is lex-smallest among w, a, b.</returns>
        private bool IsCanonical(VertexCoord w, VertexCoord a, VertexCoord b)
        {
            var wv = w.Value;
            var av = a.Value;
            var bv = b.Value;

            if (wv.x > av.x || (wv.x == av.x && wv.y > av.y))
                return false;

            if (wv.x > bv.x || (wv.x == bv.x && wv.y > bv.y))
                return false;

            return true;
        }

        /// <summary>
        ///     Converts a world XZ position to integer pixel coordinates using the square UV rect.
        /// </summary>
        /// <param name="worldXZ">World position in the XZ plane.</param>
        /// <param name="squareMin">Lower-left corner of the square UV rect.</param>
        /// <param name="squareSize">Side length of the square UV rect.</param>
        /// <param name="resolution">Texture resolution (width == height).</param>
        /// <returns>Integer pixel coordinate (column, row).</returns>
        private Vector2Int WorldToPixel(float2 worldXZ, float2 squareMin, float squareSize, int resolution)
        {
            var u = (worldXZ.x - squareMin.x) / squareSize;
            var v = (worldXZ.y - squareMin.y) / squareSize;
            return new Vector2Int(
                (int)(u * (resolution - 1)),
                (int)(v * (resolution - 1)));
        }

        /// <summary>
        ///     Rasterizes a triangle into <paramref name="pixels" /> using an integer bounding-box
        ///     sweep and a half-space edge-function test. Accepts both CW and CCW winding orders
        ///     because the flat-top axial neighbor enumeration does not guarantee a consistent winding.
        /// </summary>
        /// <param name="p0">First triangle vertex in pixel coordinates.</param>
        /// <param name="p1">Second triangle vertex in pixel coordinates.</param>
        /// <param name="p2">Third triangle vertex in pixel coordinates.</param>
        /// <param name="resolution">Texture resolution used to clamp coordinates.</param>
        /// <param name="color">Color to write to covered pixels.</param>
        /// <param name="pixels">Output pixel buffer, modified in place.</param>
        private void RasterizeTriangle(
            Vector2Int p0,
            Vector2Int p1,
            Vector2Int p2,
            int resolution,
            Color32 color,
            Color32[] pixels)
        {
            var minPx = Mathf.Max(0, Mathf.Min(p0.x, Mathf.Min(p1.x, p2.x)));
            var maxPx = Mathf.Min(resolution - 1, Mathf.Max(p0.x, Mathf.Max(p1.x, p2.x)));
            var minPy = Mathf.Max(0, Mathf.Min(p0.y, Mathf.Min(p1.y, p2.y)));
            var maxPy = Mathf.Min(resolution - 1, Mathf.Max(p0.y, Mathf.Max(p1.y, p2.y)));

            if (minPx > maxPx || minPy > maxPy)
                return;

            for (var py = minPy; py <= maxPy; py++)
            {
                for (var px = minPx; px <= maxPx; px++)
                {
                    var e0 = (px - p0.x) * (p1.y - p0.y) - (py - p0.y) * (p1.x - p0.x);
                    var e1 = (px - p1.x) * (p2.y - p1.y) - (py - p1.y) * (p2.x - p1.x);
                    var e2 = (px - p2.x) * (p0.y - p2.y) - (py - p2.y) * (p0.x - p2.x);

                    if ((e0 >= 0 && e1 >= 0 && e2 >= 0) || (e0 <= 0 && e1 <= 0 && e2 <= 0))
                        pixels[py * resolution + px] = color;
                }
            }
        }

        /// <summary>
        ///     Creates a uniform pixel array filled with the fallback color.
        ///     Used when world bounds are degenerate (zero-area terrain).
        /// </summary>
        /// <param name="totalPixels">Total pixel count.</param>
        /// <param name="fallbackColor">Fill color.</param>
        /// <returns>Uniform pixel array.</returns>
        private Color32[] BuildFallbackPixels(int totalPixels, Color fallbackColor)
        {
            var fb = new Color32(
                (byte)(fallbackColor.r * 255f),
                (byte)(fallbackColor.g * 255f),
                (byte)(fallbackColor.b * 255f),
                255);

            var pixels = new Color32[totalPixels];
            for (var i = 0; i < totalPixels; i++)
                pixels[i] = fb;

            return pixels;
        }
    }
}
