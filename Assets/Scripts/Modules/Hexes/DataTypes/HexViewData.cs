using System.Collections.Generic;
using DataTypes;
using UniRx;
using Unity.Mathematics;
using UnityEngine;

namespace Modules.Hexes.DataTypes
{
    public class HexViewData
    {
        private readonly IntReactiveProperty _level = new(0);
        private readonly ReactiveProperty<Mesh> _mesh = new();
        private readonly ReactiveProperty<Texture> _texture = new();
        public HexId HexId { get; set; }
        public IReadOnlyReactiveProperty<int> Level => _level;
        public int LevelValue => _level.Value;
        public IReadOnlyReactiveProperty<Mesh> Mesh => _mesh;

        /// <summary>
        ///     Read-only list of neighbors of the hex.
        /// </summary>
        public List<HexViewData> Neighbors { get; } = new();

        /// <summary>
        ///     Read-only list of points that make up the hex.
        /// </summary>
        public List<HexPointData> Points { get; } = new();
        public float3 Position3D { get; set; }
        public int Q => HexId.x;
        public int R => HexId.y;

        public float Size { get; set; } = 1f;

        public SurfaceType SurfaceType { get; set; }
        public IReadOnlyReactiveProperty<Texture> Texture => _texture;

        public Vector3[] Vertices => _mesh.Value.vertices;

        public HexViewData(int level, HexId hexId, float size)
        {
            HexId = hexId;
            Size = size;
            _level.Value = level;
            Position3D = CalculateTopAngleWorldPosition();
            Points.Add(new HexPointData
            {
                Type = PointType.Center,
                Position = Position3D
            });
        }

        public void AddNeighbor(HexViewData neighbor)
        {
            if (!Neighbors.Contains(neighbor))
                Neighbors.Add(neighbor);
        }

        public void AddPoint(HexPointData point)
        {
            Points.Add(point);
        }

        public float3 CalculateTopAngleWorldPosition()
        {
            var x = Size * (3.0f / 2.0f * Q);
            var z = Size * (Mathf.Sqrt(3) / 2.0f * Q + Mathf.Sqrt(3) * R);
            return new float3(x, 0, z);
        }

        public LineWithCenter GetLine(int sideIndex)
        {
            var indexA = sideIndex + 2 >= 7 ? (sideIndex + 2) % 7 + 1 : sideIndex + 2;
            var indexB = sideIndex + 3 >= 7 ? (sideIndex + 3) % 7 + 1 : sideIndex + 3;

            var a = Points[indexA].Position;
            var b = Points[indexB].Position;
            return new LineWithCenter(Position3D, a, b);
        }

        public int GetSharedLineIndex(HexViewData other)
        {
            if (HexUtil.IsNeighbour(HexId.Coords, other.HexId.Coords))
                return HexUtil.GetSharedLineIndex(other.HexId.Coords - HexId.Coords);

            return -1;
        }

        public void SetLevel(int level)
        {
            _level.SetValueAndForceNotify(level);
        }

        public void SetMesh(Mesh mesh)
        {
            _mesh.SetValueAndForceNotify(mesh);
        }

        public void SetTexture(Texture texture)
        {
            _texture.SetValueAndForceNotify(texture);
        }

        public void SetVertices(Vector3[] vertices)
        {
            var mesh = new Mesh
            {
                vertices = vertices,
                triangles = _mesh.Value.triangles,
                uv = _mesh.Value.uv,
                uv2 = _mesh.Value.uv2,
                uv3 = _mesh.Value.uv3,
                uv4 = _mesh.Value.uv4
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            SetMesh(mesh);
        }

        public BorderLine ToBorderLine()
        {
            var result = new BorderLine();

            for (var i = 2; i < Points.Count; i++)
            {
                var a = Points[i - 1].Position;
                var b = Points[i].Position;
                result.AddSegment(new LineWithCenter(Position3D, a, b), Position3D);
            }

            result.AddSegment(new LineWithCenter(Position3D, Points[^1].Position, Points[1].Position), Position3D);

            return result;
        }

        public BorderLine ToBorderLine(params int[] sidesIndexes)
        {
            var result = new BorderLine();

            foreach (var sideIndex in sidesIndexes)
            {
                if (sideIndex < 0 || sideIndex > 5)
                    continue;

                var indexA = sideIndex + 1;
                var indexB = sideIndex + 2 >= 7 ? 1 : sideIndex + 2;

                var a = Points[indexA].Position;
                var b = Points[indexB].Position;
                result.AddSegment(new LineWithCenter(Position3D, a, b), Position3D);
            }

            return result;
        }
    }
}
