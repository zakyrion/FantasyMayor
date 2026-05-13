using System.Collections.Generic;
using Modules.AxialSystem;
using Modules.TerrainView.Components;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Modules.TerrainView.Views
{
    /// <summary>
    ///     Generates a flat animated water mesh covering a set of water hex cells and exposes
    ///     a method to push stylistic parameters to the renderer via <see cref="MaterialPropertyBlock" />.
    ///     Mesh lifetime is managed by <see cref="DisposedMono" />; the prefab instance lifetime
    ///     is managed by the owning <see cref="Systems.WaterViewSubSystem" /> through its <c>Box&lt;WaterView&gt;</c>.
    /// </summary>
    public sealed class WaterView : DisposedMono
    {
        private static readonly int ShallowColorId   = Shader.PropertyToID("_ShallowColor");
        private static readonly int DeepColorId      = Shader.PropertyToID("_DeepColor");
        private static readonly int WaveNormalMapId  = Shader.PropertyToID("_WaveNormalMap");
        private static readonly int WaveSpeedId      = Shader.PropertyToID("_WaveSpeed");
        private static readonly int WaveScaleId      = Shader.PropertyToID("_WaveScale");
        private static readonly int WaveScale2Id     = Shader.PropertyToID("_WaveScale2");
        private static readonly int FlowDirectionId  = Shader.PropertyToID("_FlowDirection");
        private static readonly int FlowSpeedId      = Shader.PropertyToID("_FlowSpeed");
        private static readonly int FoamStrengthId   = Shader.PropertyToID("_FoamStrength");
        private static readonly int FoamWidthId      = Shader.PropertyToID("_FoamWidth");
        private static readonly int FresnelPowerId   = Shader.PropertyToID("_FresnelPower");
        private static readonly int TransparencyId   = Shader.PropertyToID("_Transparency");
        private static readonly int WaveAmplitudeId  = Shader.PropertyToID("_WaveAmplitude");
        private static readonly int WaveFrequencyId  = Shader.PropertyToID("_WaveFrequency");

        [SerializeField]
        private MeshFilter _meshFilter;

        private MaterialPropertyBlock _propertyBlock;
        private float _hexSize = 1f;
        private int _subdivisions = 1;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        ///     Generates the water surface mesh covering all water hexes and their shore neighbours.
        ///     Water hex vertices receive full opacity (alpha = 1); shore hex outer vertices receive
        ///     alpha = 0, producing a smooth fade at the land boundary via vertex color interpolation.
        ///     Must be called from the main thread after the prefab has been instantiated.
        /// </summary>
        /// <param name="waterHexes">Pure water hex coordinates. Caller retains ownership and must dispose.</param>
        /// <param name="shoreHexes">Land hexes adjacent to water. Caller retains ownership and must dispose.</param>
        /// <param name="hexSize">Hex cell radius in world units — must match terrain hex size.</param>
        /// <param name="waterY">World Y position of the water surface plane.</param>
        /// <param name="subdivisions">Triangle subdivisions per hex face. 1 = cheapest.</param>
        public void Generate(
            NativeHashSet<HexCoord> waterHexes,
            NativeHashSet<HexCoord> shoreHexes,
            float hexSize,
            float waterY,
            int subdivisions)
        {
            _hexSize = hexSize;
            _subdivisions = Mathf.Max(1, subdivisions);
            _meshFilter.mesh = BuildMesh(waterHexes, shoreHexes, waterY);
        }

        /// <summary>
        ///     Pushes all water shader parameters to the renderer via <see cref="MaterialPropertyBlock" />,
        ///     avoiding material instance allocation.
        ///     Must be called from the main thread.
        /// </summary>
        /// <param name="config">Flattened water config component carrying all stylistic values.</param>
        public void ApplyConfig(in WaterViewConfigComponent config)
        {
            var meshRenderer = _meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogError("[WaterView] MeshRenderer not found.");
                return;
            }

            _propertyBlock.SetColor(ShallowColorId, config.ShallowColor);
            _propertyBlock.SetColor(DeepColorId, config.DeepColor);
            _propertyBlock.SetFloat(WaveSpeedId, config.WaveSpeed);
            _propertyBlock.SetVector(WaveScaleId, new Vector4(config.WaveScale.x, config.WaveScale.y, 0f, 0f));
            _propertyBlock.SetVector(WaveScale2Id, new Vector4(config.WaveScale2.x, config.WaveScale2.y, 0f, 0f));
            _propertyBlock.SetVector(FlowDirectionId, new Vector4(config.FlowDirection.x, config.FlowDirection.y, 0f, 0f));
            _propertyBlock.SetFloat(FlowSpeedId, config.FlowSpeed);
            _propertyBlock.SetFloat(FoamStrengthId, config.FoamStrength);
            _propertyBlock.SetFloat(FoamWidthId, config.FoamWidth);
            _propertyBlock.SetFloat(FresnelPowerId, config.FresnelPower);
            _propertyBlock.SetFloat(TransparencyId, config.Transparency);
            _propertyBlock.SetFloat(WaveAmplitudeId, config.WaveAmplitude);
            _propertyBlock.SetFloat(WaveFrequencyId, config.WaveFrequency);

            if (config.WaveNormalMap != null)
                _propertyBlock.SetTexture(WaveNormalMapId, config.WaveNormalMap);

            meshRenderer.SetPropertyBlock(_propertyBlock);
        }

        // -------------------------------------------------------------------------
        // Mesh generation
        // -------------------------------------------------------------------------

        private Mesh BuildMesh(
            NativeHashSet<HexCoord> waterHexes,
            NativeHashSet<HexCoord> shoreHexes,
            float waterY)
        {
            // Pass 1: collect unique vertex XZ positions.
            // vertexAlphaMap: 1.0 = water interior, 0.0 = shore outer edge.
            // A vertex shared by both a water hex and a shore hex stays at 1.0 (water wins).
            var vertexData  = new Dictionary<Vector3Int, Vector2>();
            var vertexAlpha = new Dictionary<Vector3Int, float>();

            foreach (var hex in waterHexes)
            {
                var (center, corners) = GetHexGeometry(hex);
                for (var i = 0; i < 6; i++)
                    CollectSubdividedVertices(center, corners[i], corners[(i + 1) % 6], vertexData, vertexAlpha, 1f);
            }

            foreach (var hex in shoreHexes)
            {
                var (center, corners) = GetHexGeometry(hex);
                for (var i = 0; i < 6; i++)
                    CollectSubdividedVertices(center, corners[i], corners[(i + 1) % 6], vertexData, vertexAlpha, 0f);
            }

            // Compute isotropic UV rect spanning both water and shore hexes.
            ComputeSquareUVRect(waterHexes, shoreHexes, out var squareMin, out var squareSize);

            var vertices         = new List<Vector3>(vertexData.Count);
            var uvs              = new List<Vector2>(vertexData.Count);
            var colors           = new List<Color>(vertexData.Count);
            var vertexIndexMap   = new Dictionary<Vector3Int, int>(vertexData.Count);

            foreach (var kvp in vertexData)
            {
                var pos = kvp.Value;
                vertices.Add(new Vector3(pos.x, waterY, pos.y));
                uvs.Add(new Vector2(
                    (pos.x - squareMin.x) / squareSize,
                    (pos.y - squareMin.y) / squareSize));
                colors.Add(new Color(1f, 1f, 1f, vertexAlpha[kvp.Key]));
                vertexIndexMap[kvp.Key] = vertices.Count - 1;
            }

            // Pass 2: build triangle indices for water hexes and shore hexes.
            var triangles = new List<int>();
            foreach (var hex in waterHexes)
            {
                var (center, corners) = GetHexGeometry(hex);
                for (var i = 0; i < 6; i++)
                    BuildSubdividedTriangles(center, corners[i], corners[(i + 1) % 6], vertexIndexMap, triangles);
            }

            foreach (var hex in shoreHexes)
            {
                var (center, corners) = GetHexGeometry(hex);
                for (var i = 0; i < 6; i++)
                    BuildSubdividedTriangles(center, corners[i], corners[(i + 1) % 6], vertexIndexMap, triangles);
            }

            var mesh = new Mesh { name = "WaterSurface" };
            if (vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AddDisposable(() => mesh.Clear(true));
            return mesh;
        }

        private void ComputeSquareUVRect(
            NativeHashSet<HexCoord> waterHexes,
            NativeHashSet<HexCoord> shoreHexes,
            out Vector2 squareMin,
            out float squareSize)
        {
            var minX = float.MaxValue;
            var minZ = float.MaxValue;
            var maxX = float.MinValue;
            var maxZ = float.MinValue;

            void Expand(NativeHashSet<HexCoord> hexSet)
            {
                foreach (var hex in hexSet)
                {
                    var (center, corners) = GetHexGeometry(hex);

                    if (center.x < minX) minX = center.x;
                    if (center.x > maxX) maxX = center.x;
                    if (center.y < minZ) minZ = center.y;
                    if (center.y > maxZ) maxZ = center.y;

                    for (var i = 0; i < 6; i++)
                    {
                        if (corners[i].x < minX) minX = corners[i].x;
                        if (corners[i].x > maxX) maxX = corners[i].x;
                        if (corners[i].y < minZ) minZ = corners[i].y;
                        if (corners[i].y > maxZ) maxZ = corners[i].y;
                    }
                }
            }

            Expand(waterHexes);
            Expand(shoreHexes);

            var sizeX = maxX - minX;
            var sizeZ = maxZ - minZ;
            squareSize = Mathf.Max(sizeX, sizeZ);

            if (squareSize <= 0f)
            {
                Debug.LogError("[WaterView] Degenerate water bounds — square UV size is zero.");
                squareSize = 1f;
            }

            var centerX = (minX + maxX) * 0.5f;
            var centerZ = (minZ + maxZ) * 0.5f;
            squareMin = new Vector2(centerX - squareSize * 0.5f, centerZ - squareSize * 0.5f);
        }

        private static Vector3Int RoundToKey(Vector2 worldPos)
        {
            const float precision = 10000f;
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x * precision),
                Mathf.RoundToInt(worldPos.y * precision),
                0);
        }

        private Vector2 BarycentricPoint(Vector2 v0, Vector2 v1, Vector2 v2, int i, int j, int k)
        {
            return v0 * ((float)i / _subdivisions)
                 + v1 * ((float)j / _subdivisions)
                 + v2 * ((float)k / _subdivisions);
        }

        private void CollectSubdividedVertices(
            Vector2 v0,
            Vector2 v1,
            Vector2 v2,
            Dictionary<Vector3Int, Vector2> vertexData,
            Dictionary<Vector3Int, float> alphaMap,
            float alpha)
        {
            for (var i = 0; i <= _subdivisions; i++)
                for (var j = 0; j <= _subdivisions - i; j++)
                {
                    var point = BarycentricPoint(v0, v1, v2, i, j, _subdivisions - i - j);
                    var key   = RoundToKey(point);
                    vertexData.TryAdd(key, point);
                    // Water alpha (1.0) wins over shore alpha (0.0) on shared boundary vertices.
                    if (!alphaMap.TryGetValue(key, out var existing) || alpha > existing)
                        alphaMap[key] = alpha;
                }
        }

        private void BuildSubdividedTriangles(Vector2 v0, Vector2 v1, Vector2 v2,
            Dictionary<Vector3Int, int> vertexIndexMap, List<int> triangles)
        {
            for (var i = 0; i < _subdivisions; i++)
                for (var j = 0; j < _subdivisions - i; j++)
                {
                    var k = _subdivisions - i - j;

                    var idx0 = vertexIndexMap[RoundToKey(BarycentricPoint(v0, v1, v2, i, j, k))];
                    var idx1 = vertexIndexMap[RoundToKey(BarycentricPoint(v0, v1, v2, i + 1, j, k - 1))];
                    var idx2 = vertexIndexMap[RoundToKey(BarycentricPoint(v0, v1, v2, i, j + 1, k - 1))];

                    triangles.Add(idx0);
                    triangles.Add(idx2);
                    triangles.Add(idx1);

                    if (j < _subdivisions - i - 1)
                    {
                        var idx3 = vertexIndexMap[RoundToKey(BarycentricPoint(v0, v1, v2, i + 1, j + 1, k - 2))];
                        triangles.Add(idx1);
                        triangles.Add(idx2);
                        triangles.Add(idx3);
                    }
                }
        }

        private (Vector2 center, Vector2[] corners) GetHexGeometry(HexCoord hexCoord)
        {
            var w = AxialMath.AxialToWorld2D(hexCoord.Value, _hexSize);
            var center = new Vector2(w.x, w.y);
            var corners = new Vector2[6];
            for (var i = 0; i < 6; i++)
            {
                var angle = Mathf.Deg2Rad * (60f * i + 30f);
                corners[i] = center + new Vector2(_hexSize * Mathf.Cos(angle), _hexSize * Mathf.Sin(angle));
            }
            return (center, corners);
        }
    }
}
