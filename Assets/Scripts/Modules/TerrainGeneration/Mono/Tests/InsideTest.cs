using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class InsideTest : MonoBehaviour
{
    [SerializeField] private List<Transform> _points;
    [SerializeField] private Vector2 UV;

    // Update is called once per frame
    private void Update()
    {
        //check if point is inside triangle

        UV = InterpolateValue(((float3) transform.position).xz, new float3[]
        {
            _points[1].position,
            _points[2].position,
            _points[3].position,
            _points[4].position,
            _points[5].position,
            _points[6].position
        }, new float2[]
        {
            _points[1].GetComponent<UVHandler>().UV,
            _points[2].GetComponent<UVHandler>().UV,
            _points[3].GetComponent<UVHandler>().UV,
            _points[4].GetComponent<UVHandler>().UV,
            _points[5].GetComponent<UVHandler>().UV,
            _points[6].GetComponent<UVHandler>().UV
        });
    }


    private (float3, float3) GetIntersectPoint(float3 a, float3 b, float3 c, float3 point)
    {
        var directionA = math.normalize(b - c);
        var directionB = math.normalize(c - b);

        var intersectionB = FindIntersection(a.xz, b.xz, point.xz, (point + directionA * 100f).xz);
        var intersectionC = FindIntersection(a.xz, c.xz, point.xz, (point + directionB * 100f).xz);

        return new ValueTuple<float3, float3>(new float3(intersectionB.x, 0, intersectionB.y),
            new float3(intersectionC.x, 0, intersectionC.y));
    }

    private float2 FindIntersection(float2 A1, float2 B1, float2 A2, float2 B2)
    {
        var result = new float2(float.NaN, float.NaN); // Initialize as NaN

        var a = B1 - A1;
        var b = B2 - A2;

        var cross = Cross(a, b);

        if (math.abs(cross) < 1e-8f)
        {
            return result; // Lines are parallel or coincident, no unique intersection
        }

        var c = A2 - A1;
        var t = Cross(c, b) / cross;

        result = A1 + t * a;

        return result;
    }

    private float Cross(float2 u, float2 v) => u.x * v.y - u.y * v.x;

    private float2 GetUV(float2 A, float2 B, float2 C, float2 P, float2 a, float2 b, float2 c)
    {
        // Calculate distances from P to A, B, and C
        var dA = math.distance(P, A);
        var dB = math.distance(P, B);
        var dC = math.distance(P, C);

        // Calculate weights based on distances
        var wA = dA == 0 ? 0 : 1f / dA;
        var wB = dB == 0 ? 0 : 1f / dB;
        var wC = dC == 0 ? 0 : 1f / dC;

        // Normalize the weights
        var totalWeight = wA + wB + wC;
        wA /= totalWeight;
        wB /= totalWeight;
        wC /= totalWeight;

        // Interpolate the value at point P
        return a * wA + b * wB + c * wC;
    }


    #region HexUVInterpolation

    private float2 InterpolateValue(float2 P, float3[] vertices, float2[] valuesAtVertices)
    {
        var center = new float3(0, 0, 0);

        // Find which triangle P lies in
        for (var i = 0; i < 6; i++)
        {
            var A = vertices[i];
            var B = vertices[(i + 1) % 6];
            var C = center;

            // Check if P is inside triangle ABC
            if (IsPointInTriangle(P, A.xz, B.xz, C.xz))
            {
                var point = GetIntersectPoint(C, A, B, transform.position);

                var coef = math.distance(C, point.Item1) / math.distance(C, A);

                var UVa = math.lerp(new float2(0.5f, 0.5f), valuesAtVertices[i], coef);
                var UVb = math.lerp(new float2(0.5f, 0.5f), valuesAtVertices[(i + 1) % 6], coef);

                var coefUV = math.distance(point.Item1, new float3(P.x, 0, P.y)) /
                             math.distance(point.Item1, point.Item2);

                Debug.DrawLine(point.Item1, (Vector3) point.Item1 + Vector3.up * 10f, Color.red);
                Debug.DrawLine(point.Item2, (Vector3) point.Item2 + Vector3.up * 10f, Color.green);

                // Interpolate the value at P based on distances to A, B, and C
                return math.lerp(UVa, UVb, coefUV);
            }
        }

        return new float2(-1, -1); // Point P is not inside the hexagon
    }

    private bool IsPointInTriangle(float2 P, float2 A, float2 B, float2 C)
    {
        var d = (B.y - C.y) * (A.x - C.x) + (C.x - B.x) * (A.y - C.y);
        var u = ((B.y - C.y) * (P.x - C.x) + (C.x - B.x) * (P.y - C.y)) / d;
        var v = ((C.y - A.y) * (P.x - C.x) + (A.x - C.x) * (P.y - C.y)) / d;
        var w = 1 - u - v;

        return u >= 0 && v >= 0 && w >= 0;
    }

    private float2 InterpolateInTriangle(float2 P, float2 A, float2 B, float2 C, float2 valueAtA, float2 valueAtB,
        float2 valueAtC)
    {
        var dA = math.distance(P, A);
        var dB = math.distance(P, B);
        var dC = math.distance(P, C);

        var wA = 1f / dA;
        var wB = 1f / dB;
        var wC = 1f / dC;

        var totalWeight = wA + wB + wC;
        wA /= totalWeight;
        wB /= totalWeight;
        wC /= totalWeight;

        return valueAtA * wA + valueAtB * wB + valueAtC * wC;
    }

    #endregion
}