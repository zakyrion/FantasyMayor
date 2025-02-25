using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

public static class HexMeshUtil
{
    public static Mesh CreateFlatBasedHex(float size)
    {
        var rotator = Quaternion.AngleAxis(30, Vector3.up);
        var mesh = CreateMesh(size, rotator);

        var uv = new Vector2[mesh.vertices.Length];
        uv[0] = new Vector2(0.5f, 0.5f);
        uv[1] = new Vector2(1 - 0.866f, 1);
        uv[2] = new Vector2(0.866f, 1);
        uv[3] = new Vector2(1, .5f);
        uv[4] = new Vector2(0.866f, 0);
        uv[5] = new Vector2(1 - 0.866f, 0);
        uv[6] = new Vector2(0, 0.5f);

        mesh.uv = uv;

        var uv2 = new Vector2[mesh.vertices.Length];

        uv2[0] = new Vector2(.5f, .5f);
        uv2[1] = new Vector2(0, 1);
        uv2[2] = new Vector2(1, 1);
        uv2[3] = new Vector2(1, .5f);
        uv2[4] = new Vector2(1, 0);
        uv2[5] = new Vector2(0, 0);
        uv2[6] = new Vector2(0, 0.5f);

        mesh.uv2 = uv2;

        mesh.RecalculateNormals();

        return mesh;
    }

    public static Mesh CreatePointyBasedHex(float size)
    {
        var rotator = Quaternion.AngleAxis(60, Vector3.up);
        var mesh = CreateMesh(size, rotator);

        var uv = new Vector2[mesh.vertices.Length];
        uv[0] = new Vector2(0.5f, 0.5f);
        uv[1] = new Vector2(1 - 0.866f, 1);
        uv[2] = new Vector2(0.866f, 1);
        uv[3] = new Vector2(1, .5f);
        uv[4] = new Vector2(0.866f, 0);
        uv[5] = new Vector2(1 - 0.866f, 0);
        uv[6] = new Vector2(0, 0.5f);

        mesh.uv = uv;

        var uv2 = new Vector2[mesh.vertices.Length];

        uv2[0] = new Vector2(.5f, .5f);
        uv2[1] = new Vector2(0, 1);
        uv2[2] = new Vector2(1, 1);
        uv2[3] = new Vector2(1, .5f);
        uv2[4] = new Vector2(1, 0);
        uv2[5] = new Vector2(0, 0);
        uv2[6] = new Vector2(0, 0.5f);

        mesh.uv2 = uv2;

        mesh.RecalculateNormals();

        return mesh;
    }

    public static Mesh CreatePointyBasedHex(float size, float3 center)
    {
        var mesh = CreatePointBaseMesh(size, center);

        var uv = new Vector2[mesh.vertices.Length];

        uv[0] = new Vector2(.5f, .5f);

        uv[1] = new Vector2(1, 0.5f);
        uv[2] = new Vector2(0.866f, 0);
        uv[3] = new Vector2(1 - 0.866f, 0);
        uv[4] = new Vector2(0, 0.5f);
        uv[5] = new Vector2(1 - 0.866f, 1f);
        uv[6] = new Vector2(0.866f, 1f);

        mesh.uv = uv;

        var uv2 = new Vector2[mesh.vertices.Length];

        uv2[0] = new Vector2(.5f, .5f);

        uv2[1] = new Vector2(1f, .5f);
        uv2[2] = new Vector2(1f, 0);
        uv2[3] = new Vector2(0, 0);
        uv2[4] = new Vector2(0, .5f);
        uv2[5] = new Vector2(0, 1);
        uv2[6] = new Vector2(1, 1);

        mesh.uv2 = uv2;

        mesh.RecalculateNormals();

        return mesh;
    }

    private static Mesh CreatePointBaseMesh()
    {
        var mesh = new Mesh();

        var vertices = new List<Vector3>();
        vertices.Add(new Vector3());

        vertices.Add(new Vector3(1f, 0f, 0f)); //1
        vertices.Add(new Vector3(0.5f, 0f, -0.86602540378f)); //2
        vertices.Add(new Vector3(-0.5f, 0f, -0.86602540378f)); //3

        vertices.Add(new Vector3(-1f, 0f, 0f)); //4
        vertices.Add(new Vector3(-0.5f, 0f, 0.86602540378f)); //5
        vertices.Add(new Vector3(0.5f, 0f, 0.86602540378f)); //6 // 0.86602540378 = sqrt(3)/2

        var triangles = new[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 5,
            0, 5, 6,
            0, 6, 1
        };

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles;
        return mesh;
    }

    private static Mesh CreatePointBaseMesh(float size, float3 center)
    {
        var mesh = new Mesh();

        var vertices = new List<Vector3>();
        vertices.Add(center);

        vertices.Add((Vector3) center + new Vector3(1f, 0f, 0f) * size); //1
        vertices.Add((Vector3) center + new Vector3(0.5f, 0f, -0.86602540378f) * size); //2
        vertices.Add((Vector3) center + new Vector3(-0.5f, 0f, -0.86602540378f) * size); //3

        vertices.Add((Vector3) center + new Vector3(-1f, 0f, 0f) * size); //4
        vertices.Add((Vector3) center + new Vector3(-0.5f, 0f, 0.86602540378f) * size); //5
        vertices.Add((Vector3) center + new Vector3(0.5f, 0f, 0.86602540378f) * size); //6 // 0.86602540378 = sqrt(3)/2

        var triangles = new[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 5,
            0, 5, 6,
            0, 6, 1
        };

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles;
        return mesh;
    }

    private static Mesh CreateMesh(float size, Quaternion rotator)
    {
        var mesh = new Mesh();
        var turn = Quaternion.AngleAxis(60, Vector3.up);

        var vertices = new List<Vector3>();
        vertices.Add(new Vector3());

        vertices.Add(rotator * Vector3.forward * size);
        rotator *= turn;
        vertices.Add(rotator * Vector3.forward * size);
        rotator *= turn;
        vertices.Add(rotator * Vector3.forward * size);
        rotator *= turn;
        vertices.Add(rotator * Vector3.forward * size);
        rotator *= turn;
        vertices.Add(rotator * Vector3.forward * size);
        rotator *= turn;
        vertices.Add(rotator * Vector3.forward * size);

        var triangles = new[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 3, 4,
            0, 4, 5,
            0, 5, 6,
            0, 6, 1
        };

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles;
        return mesh;
    }

    public static Mesh DetailMesh(Mesh mesh)
    {
        void SplitUV(in List<Vector2> uvOut, Vector2[] uv, int i)
        {
            uvOut.Add(mesh.uv[mesh.triangles[i]]);
            uvOut.Add(Vector2.Lerp(uv[mesh.triangles[i]], uv[mesh.triangles[i + 1]], 0.5f));
            uvOut.Add(mesh.uv[mesh.triangles[i + 1]]);
            uvOut.Add(Vector2.Lerp(uv[mesh.triangles[i + 1]], uv[mesh.triangles[i + 2]], 0.5f));
            uvOut.Add(mesh.uv[mesh.triangles[i + 2]]);
            uvOut.Add(Vector2.Lerp(uv[mesh.triangles[i + 2]], uv[mesh.triangles[i]], 0.5f));
        }

        var newMesh = new Mesh();

        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var uv2 = new List<Vector2>();
        var triangles = new List<int>();
        var index = 0;

        for (var i = 0; i < mesh.triangles.Length; i += 3)
        {
            var pointOne = Vector3.Lerp(mesh.vertices[mesh.triangles[i]],
                mesh.vertices[mesh.triangles[i + 1]], 0.5f);
            var pointTwo = Vector3.Lerp(mesh.vertices[mesh.triangles[i + 1]],
                mesh.vertices[mesh.triangles[i + 2]], 0.5f);
            var pointThree = Vector3.Lerp(mesh.vertices[mesh.triangles[i + 2]],
                mesh.vertices[mesh.triangles[i]], 0.5f);

            SplitUV(in uv, mesh.uv, i);
            SplitUV(in uv2, mesh.uv2, i);

            vertices.Add(mesh.vertices[mesh.triangles[i]]);
            vertices.Add(pointOne);
            vertices.Add(mesh.vertices[mesh.triangles[i + 1]]);
            vertices.Add(pointTwo);
            vertices.Add(mesh.vertices[mesh.triangles[i + 2]]);
            vertices.Add(pointThree);

            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 5);

            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index + 3);

            triangles.Add(index + 1);
            triangles.Add(index + 3);
            triangles.Add(index + 5);

            triangles.Add(index + 5);
            triangles.Add(index + 3);
            triangles.Add(index + 4);

            index += 6;
        }

        newMesh.vertices = vertices.ToArray();
        newMesh.triangles = triangles.ToArray();
        newMesh.uv = uv.ToArray();
        newMesh.uv2 = uv2.ToArray();
        newMesh.RecalculateNormals();

        return newMesh;
    }

    [BurstCompile]
    public static int VerticesCountInWave(int wave) => wave switch
    {
        0 => 1,
        _ => 6 * wave
    };

    [BurstCompile]
    public static int FirstVertexInWave(int wave) => wave switch
    {
        0 => 0,
        _ => VerticesCountInHex(wave - 1)
    };

    /// <summary>
    ///     Calculates the wave number based on the given vertex number.
    /// </summary>
    /// <param name="vertexIndex">The vertex number.</param>
    /// <returns>The wave number.</returns>
    [BurstCompile]
    public static int WaveByVertex(int vertexIndex)
    {
        int a = 3, b = 3, c = 1 - vertexIndex; // Rearranged equation according to quadratic formula

        double discriminant = math.sqrt(b * b - 4 * a * c);

        var wave = (int) (-b + discriminant) / (2 * a) + 1;

        return wave;
    }

    [BurstCompile]
    public static int WaveByVertex(int vertexIndex, int startFromWave)
    {
        do
        {
            var count = VerticesCountInHex(startFromWave);
            
            if (vertexIndex >= count) 
                return startFromWave + 1;
            
            startFromWave--;
        } while (startFromWave > 0);

        return 0;
    }

    [BurstCompile]
    public static int VerticesCountInHex(int wave) => 1 + 3 * wave * wave + 3 * wave;
}