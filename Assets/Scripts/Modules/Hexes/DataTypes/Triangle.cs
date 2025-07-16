using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public struct Triangle
{
    public float3 A;
    public float3 B;
    public float3 C;

    public float3 this[int i] => i switch
    {
        0 => A,
        1 => B,
        2 => C,
        _ => throw new ArgumentOutOfRangeException(nameof(i), i, null)
    };

    public Triangle(float3 a, float3 b, float3 c)
    {
        A = a;
        B = b;
        C = c;
    }

    public Rect Rect()
    {
        var xmin = math.min(C.x, math.min(A.x, B.x));
        var xmax = math.max(C.x, math.max(A.x, B.x));

        var zmin = math.min(C.z, math.min(A.z, B.z));
        var zmax = math.max(C.z, math.max(A.z, B.z));

        return new Rect(xmin, zmin, xmax - xmin, zmax - zmin);
    }

    public bool Inside(float3 point)
    {
        var v0 = C - A;
        var v1 = B - A;
        var v2 = point - A;

        var dot00 = math.dot(v0, v0);
        var dot01 = math.dot(v0, v1);
        var dot02 = math.dot(v0, v2);
        var dot11 = math.dot(v1, v1);
        var dot12 = math.dot(v1, v2);

        var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return u >= 0 && v >= 0 && u + v < 1;
    }

    public bool Inside(float2 point)
    {
        var v0 = C.xz - A.xz; // Use only x and z components
        var v1 = B.xz - A.xz;
        var v2 = point - A.xz;

        var dot00 = math.dot(v0, v0);
        var dot01 = math.dot(v0, v1);
        var dot02 = math.dot(v0, v2);
        var dot11 = math.dot(v1, v1);
        var dot12 = math.dot(v1, v2);

        var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        return u >= 0 && v >= 0 && u + v < 1;
    }

    public float3 GetNormal()
    {
        var v1 = B - A;
        var v2 = C - A;
        return math.normalize(math.cross(v1, v2));
    }

    public float3 GetNormal2D()
    {
        var a = new float3(A.x, 0, A.z);
        var b = new float3(B.x, 0, B.z);
        var c = new float3(C.x, 0, C.z);

        var v1 = b - a;
        var v2 = c - a;
        return math.normalize(math.cross(v1, v2));
    }

    public float GetHeightAtPoint(float2 point)
    {
        // calculate the plane
        var normal = GetNormal2D();

        var d = math.dot(normal, A);

        // projection of the point onto the plane of the triangle
        var projectedPoint = new float3(point.x, 0, point.y) -
                             (math.dot(normal, new float3(point.x, 0, point.y)) - d) * normal;

        // calculate barycentric coordinates
        float3 v0 = B - A, v1 = C - A, v2 = projectedPoint - A;

        v0.y = 0;
        v1.y = 0;
        v2.y = 0;

        var d00 = math.dot(v0, v0);
        var d01 = math.dot(v0, v1);
        var d11 = math.dot(v1, v1);
        var d20 = math.dot(v2, v0);
        var d21 = math.dot(v2, v1);
        var denom = d00 * d11 - d01 * d01;
        var v = (d11 * d20 - d01 * d21) / denom;
        var w = (d00 * d21 - d01 * d20) / denom;
        var u = 1.0f - v - w;

        // calculate the height by using barycentric coordinates
        return u * A.y + v * B.y + w * C.y;
    }

    public bool ContainsVertex(float3 vertex) => A.Equals(vertex) || B.Equals(vertex) || C.Equals(vertex);

    public void Draw(Color color, float duration = 10)
    {
        Debug.DrawLine(A, B, color, duration);
        Debug.DrawLine(B, C, color, duration);
        Debug.DrawLine(C, A, color, duration);
    }

    public void DrawOnFlat(Color color, float duration = 10)
    {
        Debug.DrawLine(new Vector3(A.x, 0, A.z), new Vector3(B.x, 0, B.z), color, duration);
        Debug.DrawLine(new Vector3(B.x, 0, B.z), new Vector3(C.x, 0, C.z), color, duration);
        Debug.DrawLine(new Vector3(C.x, 0, C.z), new Vector3(A.x, 0, A.z), color, duration);
    }
}

public struct TriangleI3
{
    public int IndexA { get; set; }
    public int IndexB { get; set; }
    public int IndexC { get; set; }
}

public struct VertexIndexed
{
    public float2 Vertex { get; }
    public int Index { get; }

    public VertexIndexed(float2 vertex)
    {
        Vertex = vertex;
        Index = -1;
    }

    public VertexIndexed(float2 vertex, int index)
    {
        Vertex = vertex;
        Index = index;
    }
}

public struct TriangleIndexed
{
    public VertexIndexed A { get; }
    public VertexIndexed B { get; }
    public VertexIndexed C { get; }

    public TriangleIndexed(VertexIndexed a, VertexIndexed b, VertexIndexed c)
    {
        A = a;
        B = b;
        C = c;
    }
}