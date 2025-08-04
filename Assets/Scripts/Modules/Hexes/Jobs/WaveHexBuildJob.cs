using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct WaveHexBuildJob : IJob
{
    public int Waves;
    public NativeArray<float3> VerticesIn;
    public NativeArray<float2> UvsIn;
    public NativeArray<float2> Uvs2In;

    public NativeList<float3> VerticesOut;
    public NativeList<int> TrianglesOut;
    public NativeList<float2> UVsOut;
    public NativeList<float2> UVs2Out;
    public NativeList<float2> UVs3Out;
    public NativeList<float2> UVs4Out;

    public void Execute()
    {
        CreateVertices();
        JoinFirstWave();
        JoinOtherWaves();
        CreateHeightMap();
    }

    private void CreateVertices()
    {
        VerticesOut.Add(VerticesIn[0]);
        UVsOut.Add(UvsIn[0]);
        UVs2Out.Add(Uvs2In[0]);
        UVs3Out.Add(Waves);

        for (var i = 0; i < Waves; i++)
        {
            var direction = math.normalize(VerticesIn[1] - VerticesIn[0]);
            var stepSize = math.distance(VerticesIn[0], VerticesIn[1]) / Waves;
            var point = VerticesIn[0] + direction * stepSize * (i + 1);
            VerticesOut.Add(point);
            UVs3Out.Add(new float2(Waves - i, 0));

            var vertexC = VerticesIn[0];
            var vertexA = VerticesIn[1];
            var vertexB = VerticesIn[2];

            InterpolateValue(point, vertexA, vertexB, vertexC, UvsIn[1], UvsIn[2],
                Uvs2In[1], Uvs2In[2]);

            for (var j = 1; j < 7; j++)
            {
                var delta = j == 6 ? VerticesIn[1] - VerticesIn[6] : VerticesIn[j + 1] - VerticesIn[j];

                vertexA = VerticesIn[j];
                vertexB = j == 6 ? VerticesIn[1] : VerticesIn[j + 1];

                //TODO add UVs calculation
                direction = math.normalize(delta);
                stepSize = math.length(delta) / Waves;

                var countOfNewVertices = j == 6 ? i + 1 : i + 2;

                for (var k = 1; k < countOfNewVertices; k++)
                {
                    point = VerticesOut[VerticesOut.Length - 1] + direction * stepSize;
                    VerticesOut.Add(point);
                    UVs3Out.Add(new float2(Waves - i, 0));

                    //var centroid = (vertexA + vertexB + vertexC) / 3;

                    //vertexA = vertexA + (vertexA - centroid) * 0.01f;
                    //vertexB = vertexB + (vertexB - centroid) * 0.01f;
                    //vertexC = vertexC + (vertexC - centroid) * 0.01f;

                    InterpolateValue(point, vertexA, vertexB, vertexC, UvsIn[j], UvsIn[j == 6 ? 1 : j + 1],
                        Uvs2In[j], Uvs2In[j == 6 ? 1 : j + 1]);
                }
            }
        }
    }

    private void InterpolateValue(float3 point, float3 A, float3 B, float3 C, float2 valA, float2 valB, float2 val2a,
        float2 val2b)
    {
        var intersectPoint = GetIntersectPoint(C, A, B, point);

        var coef = math.distance(C.xz, intersectPoint.Item1.xz) / math.distance(C.xz, A.xz);

        var UVa = math.lerp(new float2(0.5f, 0.5f), valA, coef);
        var UVb = math.lerp(new float2(0.5f, 0.5f), valB, coef);

        var UV2a = math.lerp(new float2(0.5f, 0.5f), val2a, coef);
        var UV2b = math.lerp(new float2(0.5f, 0.5f), val2b, coef);

        var coefUV = math.distance(intersectPoint.Item1.xz, new float2(point.x, point.z)) /
                     math.distance(intersectPoint.Item1.xz, intersectPoint.Item2.xz);

        // Interpolate the value at P based on distances to A, B, and C
        var uv1 = math.lerp(UVa, UVb, coefUV);
        var uv2 = math.lerp(UV2a, UV2b, coefUV);

        UVsOut.Add(uv1);
        UVs2Out.Add(uv2);
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

    private void JoinFirstWave()
    {
        var triangles = new[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 5,
            0, 5, 6,
            0, 6, 1
        };

        for (var i = 0; i < triangles.Length; i += 3)
        {
            TrianglesOut.Add(triangles[i]);
            TrianglesOut.Add(triangles[i + 1]);
            TrianglesOut.Add(triangles[i + 2]);
        }
    }

    private void JoinOtherWaves()
    {
        if (Waves > 1)
        {
            var begin = 0;
            var end = 1;
            var trianglesPerSide = 3;
            var countOfVertices = 6;

            for (var i = 1; i < Waves; i++)
            {
                countOfVertices += (i + 1) * 6;
                var step = 6 * i;
                end += step;
                begin = end - step;
                var countOfTriangles = trianglesPerSide * 6;

                for (var j = 0; j < countOfTriangles;)
                {
                    for (var k = 0; k < trianglesPerSide; k++)
                    {
                        j++;

                        if (j == countOfTriangles)
                        {
                            TrianglesOut.Add(end - 6 * i);
                            TrianglesOut.Add(countOfVertices);
                            TrianglesOut.Add(end);
                        }
                        else if (j + 1 == countOfTriangles)
                        {
                            TrianglesOut.Add(begin);
                            TrianglesOut.Add(countOfVertices);
                            TrianglesOut.Add(end - 6 * i);
                        }
                        else if (k % 2 != 0)
                        {
                            TrianglesOut.Add(begin);
                            TrianglesOut.Add(begin + step + 1);
                            TrianglesOut.Add(begin + 1);
                            begin++;
                        }
                        else
                        {
                            TrianglesOut.Add(begin);
                            TrianglesOut.Add(begin + step);
                            TrianglesOut.Add(begin + step + 1);
                        }
                    }

                    step++;
                }

                trianglesPerSide += 2;
            }
        }
    }

    private void CreateHeightMap()
    {
        var zeroWaves = 5;
        var maxWaveValue = Waves - zeroWaves;

        for (var i = 0; i < VerticesOut.Length; i++)
        {
            var waveValue = UVs3Out[i].x <= zeroWaves ? 0 : (UVs3Out[i].x - zeroWaves) / maxWaveValue;
            UVs3Out[i] = new float2(waveValue, 0);
            //TODO use y for this and lepr function to protect apply height greater than it`s possible for the specific terrain type
            UVs4Out.Add(new float2(VerticesOut[i].y * (waveValue / 2), 0));
        }
    }
}