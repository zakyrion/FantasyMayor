using System.Collections.Generic;
using System.Linq;
using TriangleNet;
using TriangleNet.Geometry;
using TriangleNet.Meshing.Algorithm;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class Stripe
{
    private readonly List<float3> _centres = new();
    private readonly List<IsoLine> _lines = new();

    private readonly List<Vertex> _vertices = new();
    private readonly List<float3> _verticesF3 = new();

    public Stripe(int segmentsCount, params BorderLine[] borderLines)
    {
        SegmentCount = segmentsCount;

        for (var i = 0; i < borderLines.Length; i++)
        {
            var isoLine = new IsoLine(_verticesF3, _vertices, _centres);
            var borderLine = borderLines[i];
            var segments = borderLines[i].Segments;

            for (var j = 0; j < SegmentCount; j++)
            {
                var point = borderLine.InterpolatePointOnLine((float) j / SegmentCount);

                foreach (var segment in segments)
                {
                    if (segment.IsPointBetween(point))
                    {
                        var vertexIndex = AddVertex(point);
                        _centres.Add(segment.CenterOfHexTriangle);
                        isoLine.AddSegment(vertexIndex);
                        break;
                    }
                }
            }

            _lines.Add(isoLine);
        }

        int AddVertex(float3 position)
        {
            var index = _verticesF3.FindIndex(v => math.distance(v.xz, position.xz) <= .01f);

            if (index == -1)
            {
                index = _vertices.Count;
                _verticesF3.Add(position);
                _vertices.Add(new Vertex(position.x, position.z));
            }

            return index;
        }
    }

    public int SegmentCount { get; }

    public float3 GetPointInside() => _lines[0].GetCenter(0);

    public List<Triangle> Triangles()
    {
        var result = new List<Triangle>();
        var triangulator = new SweepLine();

        var bigBorders = _lines[^1].ToVertexList();

        var mesh = triangulator.Triangulate(_vertices, new Configuration());

        foreach (var triangle in mesh.Triangles)
        {
            var testVertices = new List<Vertex> {triangle.GetVertex(0), triangle.GetVertex(1), triangle.GetVertex(2)};

            if (testVertices.Count(tv => bigBorders.Any(bv => bv.Equals(tv))) != 3)
            {
                var a = _verticesF3[_vertices.IndexOf(triangle.GetVertex(0))];
                var b = _verticesF3[_vertices.IndexOf(triangle.GetVertex(1))];
                var c = _verticesF3[_vertices.IndexOf(triangle.GetVertex(2))];

                result.Add(new Triangle(a, b, c));
            }
        }

        return result;
    }

    public void ShiftLine(int index, AnimationCurve shiftPattern, float offset)
    {
        var isoLine = _lines[index];
        var maxTime = shiftPattern.keys[^1].time;

        for (var i = 0; i < SegmentCount; i++)
        {
            var lerp = (float) i / SegmentCount;
            var shift = offset * math.clamp(shiftPattern.Evaluate(lerp * maxTime), -1, 1);
            isoLine.Shift(i, shift);
        }
    }

    public void ShiftAllPoints(AnimationCurve shiftPattern, float offset)
    {
        var maxTime = shiftPattern.keys[^1].time;

        foreach (var isoLine in _lines)
        {
            for (var i = 0; i < SegmentCount; i++)
            {
                var lerp = (float) i / SegmentCount;
                var shift = offset * shiftPattern.Evaluate(lerp * maxTime);
                isoLine.Shift(i, shift);
            }
        }
    }

    public NativeArray<Triangle> ToNativeArray(Allocator allocator = Allocator.Temp) =>
        Triangles().ToNativeArray(allocator);

    public void Draw(Color color, float duration = 10)
    {
        foreach (var line in _lines) line.Draw(color, duration);
        return;
        var triangles = Triangles();

        foreach (var triangle in triangles) triangle.Draw(color, duration);
    }

    public void DrawOnFlat(Color color, float duration = 10)
    {
        var triangles = Triangles();
        foreach (var triangle in triangles) triangle.DrawOnFlat(color, duration);
    }

    public Rect Rect()
    {
        var minX = _verticesF3.Min(v => v.x);
        var minY = _verticesF3.Min(v => v.z);

        var maxX = _verticesF3.Max(v => v.x);
        var maxY = _verticesF3.Max(v => v.z);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private class IsoLine
    {
        private readonly List<float3> _centres = new();
        private readonly List<int> _indexes = new();
        private readonly List<Vertex> _vertices = new();
        private readonly List<float3> _verticesF3 = new();

        public IsoLine(List<float3> verticesf3, List<Vertex> vertices, List<float3> centres)
        {
            _verticesF3 = verticesf3;
            _vertices = vertices;
            _centres = centres;
        }

        public void Shift(int index, float offset)
        {
            var point = GetPoint(index);
            var center = GetCenter(index);

            var shift = math.normalize(center - point) * offset;
            shift.y = 0;
            point += shift;

            _verticesF3[GetIndex(index)] = point;
            _vertices[GetIndex(index)] = new Vertex(point.x, point.z);
        }

        public float3 GetCenter(int index) => _centres[_indexes[index]];
        public float3 GetPoint(int index) => _verticesF3[_indexes[index]];
        public int GetIndex(int index) => _indexes[index];
        public void AddSegment(int vertexIndex) => _indexes.Add(vertexIndex);
        public override string ToString() => _verticesF3.Count.ToString();
        public List<float3> ToF3List() => _indexes.Select(s => _verticesF3[s]).ToList();
        public List<float2> ToF2List() => _indexes.Select(s => _verticesF3[s].xz).ToList();
        public List<Vertex> ToVertexList() => _indexes.Select(s => _vertices[s]).ToList();

        public void Draw(Color color, float duration = 10)
        {
            for (var i = 0; i < _indexes.Count; i++)
            {
                var next = i + 1 >= _indexes.Count ? 0 : i + 1;

                Debug.DrawLine(GetPoint(i), GetPoint(next), color, duration);
            }
        }
    }
}