using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(MeshFilter))]
public class TriangleSplitter : MonoBehaviour
{
    [SerializeField] private MeshFilter _meshFilter;

    [FormerlySerializedAs("sideLength")] [SerializeField]
    private float _sideLength = 1f;

    [SerializeField] private int _detailLevel = 1;
    [SerializeField] private bool _isFlatBased = true;

    // Start is called before the first frame update
    private void Start()
    {
        if (_isFlatBased)
        {
            _meshFilter.mesh = HexMeshUtil.CreateFlatBasedHex(_sideLength);
        }
        else
        {
            _meshFilter.mesh = HexMeshUtil.CreatePointyBasedHex(_sideLength, transform.position);
        }

        StartCoroutine(WaitForWaveJob());
    }

    //create a coroutine that waits for the job to complete
    private IEnumerator WaitForJob()
    {
        //for (var i = 0; i < _detailLevel; i++)
        {
            var mesh = _meshFilter.mesh;

            var vertices = new NativeList<float3>(mesh.vertices.Length, Allocator.TempJob);
            var triangles = new NativeList<int>(mesh.triangles.Length, Allocator.TempJob);
            var uvs = new NativeList<float2>(mesh.uv.Length, Allocator.TempJob);
            var uvs2 = new NativeList<float2>(mesh.uv2.Length, Allocator.TempJob);

            var verticesOut = new NativeList<float3>(Allocator.TempJob);
            var trianglesOut = new NativeList<int>(Allocator.TempJob);
            var uvsOut = new NativeList<float2>(Allocator.TempJob);
            var uvs2Out = new NativeList<float2>(Allocator.TempJob);

            for (var j = 0; j < mesh.vertices.Length; j++)
            {
                vertices.Add(mesh.vertices[j]);
            }

            for (var j = 0; j < mesh.triangles.Length; j++)
            {
                triangles.Add(mesh.triangles[j]);
            }

            for (var j = 0; j < mesh.uv.Length; j++)
            {
                uvs.Add(mesh.uv[j]);
            }

            for (var j = 0; j < mesh.uv2.Length; j++)
            {
                uvs2.Add(mesh.uv2[j]);
            }

            var job = new MeshSplitJob
            {
                Triangles = triangles,
                Vertices = vertices,
                Uvs = uvs,
                Uvs2 = uvs2,
                TrianglesOut = trianglesOut,
                VerticesOut = verticesOut,
                UvsOut = uvsOut,
                Uvs2Out = uvs2Out,
                DetailLevel = _detailLevel
            };

            var handler = job.Schedule();
            //yield return new WaitWhile(() => !handler.IsCompleted);

            while (!handler.IsCompleted)
            {
                yield return new WaitForSeconds(.1f);
            }

            handler.Complete();
            var newMesh = new Mesh();

            var vertList = new List<Vector3>();
            var triList = new List<int>();
            var uvList = new List<Vector2>();
            var uv2List = new List<Vector2>();

            for (var j = 0; j < verticesOut.Length; j++)
            {
                vertList.Add(verticesOut[j]);
            }

            for (var j = 0; j < trianglesOut.Length; j++)
            {
                triList.Add(trianglesOut[j]);
            }

            for (var j = 0; j < uvsOut.Length; j++)
            {
                uvList.Add(uvsOut[j]);
            }

            for (var j = 0; j < uvs2Out.Length; j++)
            {
                uv2List.Add(uvs2Out[j]);
            }

            newMesh.SetVertices(vertList);
            newMesh.SetTriangles(triList, 0);
            newMesh.SetUVs(0, uvList);
            newMesh.SetUVs(1, uv2List);

            newMesh.RecalculateNormals();
            _meshFilter.mesh = newMesh;

            vertices.Dispose();
            triangles.Dispose();
            uvs.Dispose();
            uvs2.Dispose();

            verticesOut.Dispose();
            trianglesOut.Dispose();
            uvsOut.Dispose();
            uvs2Out.Dispose();
        }
    }

    private IEnumerator WaitForWaveJob()
    {
        var mesh = _meshFilter.mesh;

        var vertices = new NativeList<float3>(mesh.vertices.Length, Allocator.TempJob);
        var triangles = new NativeList<int>(mesh.triangles.Length, Allocator.TempJob);
        var uvs = new NativeList<float2>(mesh.uv.Length, Allocator.TempJob);
        var uvs2 = new NativeList<float2>(mesh.uv2.Length, Allocator.TempJob);

        var verticesOut = new NativeList<float3>(Allocator.TempJob);
        var trianglesOut = new NativeList<int>(Allocator.TempJob);
        var uvsOut = new NativeList<float2>(Allocator.TempJob);
        var uvs2Out = new NativeList<float2>(Allocator.TempJob);

        for (var j = 0; j < mesh.vertices.Length; j++)
        {
            vertices.Add(mesh.vertices[j]);
        }

        for (var j = 0; j < mesh.triangles.Length; j++)
        {
            triangles.Add(mesh.triangles[j]);
        }

        for (var j = 0; j < mesh.uv.Length; j++)
        {
            uvs.Add(mesh.uv[j]);
        }

        for (var j = 0; j < mesh.uv2.Length; j++)
        {
            uvs2.Add(mesh.uv2[j]);
        }

        var job = new WaveHexBuildJob
        {
            Waves = _detailLevel,
            VerticesIn = vertices,
            TrianglesOut = trianglesOut,
            VerticesOut = verticesOut
        };

        var handler = job.Schedule();
        //yield return new WaitWhile(() => !handler.IsCompleted);

        while (!handler.IsCompleted)
        {
            yield return new WaitForSeconds(.1f);
        }

        handler.Complete();

        var newMesh = new Mesh();

        var vertList = new List<Vector3>();
        var triList = new List<int>();
        var uvList = new List<Vector2>();
        var uv2List = new List<Vector2>();

        for (var j = 0; j < verticesOut.Length; j++)
        {
            vertList.Add(verticesOut[j]);
        }

        for (var j = 0; j < trianglesOut.Length; j++)
        {
            triList.Add(trianglesOut[j]);
        }

        for (var j = 0; j < uvsOut.Length; j++)
        {
            uvList.Add(uvsOut[j]);
        }

        for (var j = 0; j < uvs2Out.Length; j++)
        {
            uv2List.Add(uvs2Out[j]);
        }

        newMesh.SetVertices(vertList);
        newMesh.SetTriangles(triList, 0);
        newMesh.SetUVs(0, uvList);
        newMesh.SetUVs(1, uv2List);

        newMesh.RecalculateNormals();
        _meshFilter.mesh = newMesh;

        vertices.Dispose();
        triangles.Dispose();
        uvs.Dispose();
        uvs2.Dispose();

        verticesOut.Dispose();
        trianglesOut.Dispose();
        uvsOut.Dispose();
        uvs2Out.Dispose();
    }
}