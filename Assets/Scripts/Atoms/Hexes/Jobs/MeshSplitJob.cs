using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct MeshSplitJob : IJob
{
    public NativeList<float3> Vertices;
    public NativeList<int> Triangles;
    public NativeList<float2> Uvs;
    public NativeList<float2> Uvs2;
    public NativeList<float2> Uvs3;
    public NativeList<float2> Uvs4;

    public NativeList<float3> VerticesOut;
    public NativeList<int> TrianglesOut;
    public NativeList<float2> UvsOut;
    public NativeList<float2> Uvs2Out;
    public NativeList<float2> Uvs3Out;
    public NativeList<float2> Uvs4Out;

    public int DetailLevel;

    public void Execute()
    {
        for (var l = 0; l < DetailLevel; l++)
        {
            VerticesOut.Clear();
            TrianglesOut.Clear();
            UvsOut.Clear();
            Uvs2Out.Clear();

            var triangles = Triangles;

            void SplitUV(in NativeList<float2> uvOut, in NativeArray<float2> uv, int i)
            {
                uvOut.Add(uv[triangles[i]]);
                uvOut.Add(math.lerp(uv[triangles[i]], uv[triangles[i + 1]], 0.5f));
                uvOut.Add(uv[triangles[i + 1]]);
                uvOut.Add(math.lerp(uv[triangles[i + 1]], uv[triangles[i + 2]], 0.5f));
                uvOut.Add(uv[triangles[i + 2]]);
                uvOut.Add(math.lerp(uv[triangles[i + 2]], uv[triangles[i]], 0.5f));
            }

            var index = 0;

            for (var i = 0; i < Triangles.Length; i += 3)
            {
                var pointOne = math.lerp(Vertices[Triangles[i]],
                    Vertices[Triangles[i + 1]], 0.5f);
                var pointTwo = math.lerp(Vertices[Triangles[i + 1]],
                    Vertices[Triangles[i + 2]], 0.5f);
                var pointThree = math.lerp(Vertices[Triangles[i + 2]],
                    Vertices[Triangles[i]], 0.5f);

                SplitUV(in UvsOut, Uvs, i);
                SplitUV(in Uvs2Out, Uvs2, i);
                SplitUV(in Uvs3Out, Uvs3, i);
                SplitUV(in Uvs4Out, Uvs4, i);

                VerticesOut.Add(Vertices[Triangles[i]]);
                VerticesOut.Add(pointOne);
                VerticesOut.Add(Vertices[Triangles[i + 1]]);
                VerticesOut.Add(pointTwo);
                VerticesOut.Add(Vertices[Triangles[i + 2]]);
                VerticesOut.Add(pointThree);

                TrianglesOut.Add(index);
                TrianglesOut.Add(index + 1);
                TrianglesOut.Add(index + 5);

                TrianglesOut.Add(index + 1);
                TrianglesOut.Add(index + 2);
                TrianglesOut.Add(index + 3);

                TrianglesOut.Add(index + 1);
                TrianglesOut.Add(index + 3);
                TrianglesOut.Add(index + 5);

                TrianglesOut.Add(index + 5);
                TrianglesOut.Add(index + 3);
                TrianglesOut.Add(index + 4);

                index += 6;
            }

            Vertices.Clear();
            Triangles.Clear();
            Uvs.Clear();
            Uvs2.Clear();

            Vertices.AddRange(VerticesOut);
            Triangles.AddRange(TrianglesOut);
            Uvs.AddRange(UvsOut);
            Uvs2.AddRange(Uvs2Out);
        }
    }
}