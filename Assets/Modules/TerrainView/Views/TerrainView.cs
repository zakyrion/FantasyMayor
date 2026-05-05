using System.Collections.Generic;
using Modules.AxialSystem;
using Modules.HexesCore.Utils;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Modules.TerrainView.Views
{
    public class TerrainView : DisposedMono
    {
        [SerializeField]
        private MeshFilter _meshFilter;

        private float _hexSize = 1f;
        private int _subdivisions = 2;

        public void ApplyHeightsFromVertexGrid(VertexGrid vertexGrid)
        {
            var mesh = _meshFilter.mesh;
            var vertices = mesh.vertices;

            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var axial = vertexGrid.WorldToAxial(new float3(v.x, 0f, v.z));
                if (vertexGrid.TryGet(axial, out var hexVertex))
                    vertices[i].y = hexVertex.Position.y;
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        /// <summary>Generates the hex terrain mesh for the given set of hex coordinates.</summary>
        /// <param name="hexCoords">Set of hex coordinates to tessellate. Caller retains ownership and must dispose.</param>
        /// <param name="hexSize">Hex cell radius in world units.</param>
        /// <param name="subdivisions">Number of triangle subdivisions per hex face.</param>
        public void Generate(NativeHashSet<HexCoord> hexCoords, float hexSize = 1f, int subdivisions = 2)
        {
            _hexSize = hexSize;
            _subdivisions = subdivisions;
            _meshFilter.mesh = GenerateMesh(hexCoords);
        }

        private static Bounds CalculateBounds(IEnumerable<Vector2> positions)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);

            foreach (var pos in positions)
            {
                min = Vector2.Min(min, pos);
                max = Vector2.Max(max, pos);
            }

            var center = (min + max) * 0.5f;
            var size = max - min;
            return new Bounds(new Vector3(center.x, 0, center.y), new Vector3(size.x, 0, size.y));
        }

        private static Vector3Int RoundToKey(Vector2 worldPos)
        {
            const float precision = 10000f;
            return new Vector3Int(
                Mathf.RoundToInt(worldPos.x * precision),
                Mathf.RoundToInt(worldPos.y * precision),
                0);
        }

        // -------------------------------------------------------------------------
        // Math utilities
        // -------------------------------------------------------------------------

        private Vector2 BarycentricPoint(Vector2 v0, Vector2 v1, Vector2 v2, int i, int j, int k)
        {
            return v0 * ((float)i / _subdivisions)
                + v1 * ((float)j / _subdivisions)
                + v2 * ((float)k / _subdivisions);
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

        private void CollectSubdividedVertices(Vector2 v0, Vector2 v1, Vector2 v2,
            Dictionary<Vector3Int, Vector2> vertexData)
        {
            for (var i = 0; i <= _subdivisions; i++)
                for (var j = 0; j <= _subdivisions - i; j++)
                {
                    var point = BarycentricPoint(v0, v1, v2, i, j, _subdivisions - i - j);
                    vertexData.TryAdd(RoundToKey(point), point);
                }
        }

        private Mesh GenerateMesh(NativeHashSet<HexCoord> hexCoords)
        {
            // Pass 1: collect unique vertex positions
            var vertexData = new Dictionary<Vector3Int, Vector2>();
            foreach (var hex in hexCoords)
            {
                var (center, corners) = GetHexGeometry(hex);
                for (var i = 0; i < 6; i++)
                    CollectSubdividedVertices(center, corners[i], corners[(i + 1) % 6], vertexData);
            }

            // Build vertex + UV arrays
            var bounds = CalculateBounds(vertexData.Values);
            var boundsMin = new Vector2(bounds.min.x, bounds.min.z);
            var boundsSize = new Vector2(bounds.size.x, bounds.size.z);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var vertexIndexMap = new Dictionary<Vector3Int, int>();

            foreach (var kvp in vertexData)
            {
                var pos = kvp.Value;
                vertices.Add(new Vector3(pos.x, 0f, pos.y));
                uvs.Add(new Vector2((pos.x - boundsMin.x) / boundsSize.x, (pos.y - boundsMin.y) / boundsSize.y));
                vertexIndexMap[kvp.Key] = vertices.Count - 1;
            }

            // Pass 2: build triangle indices
            var triangles = new List<int>();
            foreach (var hex in hexCoords)
            {
                var (center, corners) = GetHexGeometry(hex);
                for (var i = 0; i < 6; i++)
                    BuildSubdividedTriangles(center, corners[i], corners[(i + 1) % 6], vertexIndexMap, triangles);
            }

            var mesh = new Mesh { name = "HexTerrain" };
            if (vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            AddDisposable(() => mesh.Clear(true));

            return mesh;
        }

        // -------------------------------------------------------------------------
        // Hex geometry
        // -------------------------------------------------------------------------

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
