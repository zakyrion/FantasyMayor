using System.Collections.Generic;
using Atoms.Hexes.DataTypes;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

public class HexMonoFactory : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private GameObject _hexPrefab;
    [SerializeField] private bool _needToApplyMaterial;
    [SerializeField] private Material _material;
    private DiContainer _resolver;

    [Inject]
    private void Init(DiContainer resolver)
    {
        _resolver = resolver;
    }

    public async void SpawnHex(NativeList<float3> vertices)
    {
        var hex = Instantiate(_hexPrefab);

        if (_needToApplyMaterial)
        {
            var renderer = hex.GetComponent<MeshRenderer>();
            renderer.material = _material;
        }

        var meshFilter = hex.GetComponent<MeshFilter>();
        meshFilter.mesh = HexMeshUtil.CreatePointyBasedHex(1, vertices[0]);

        await WaitForWaveJob(vertices, meshFilter, 1);
    }

    public async UniTask<bool> SpawnHex(HexViewData hex, int detailLevel)
    {
        var vertices = new NativeList<float3>(Allocator.TempJob);

        foreach (var hexPoint in hex.Points) vertices.Add(hexPoint.Position);

        var hexGo = _resolver.InstantiatePrefab(_hexPrefab, _root.transform);
        hexGo.GetComponent<HexView>().Init(hex);
        hexGo.name = $"Hex_({hex.HexId.x}:{hex.HexId.y})";

        if (_needToApplyMaterial)
        {
            var renderer = hexGo.GetComponent<MeshRenderer>();
            renderer.material = _material;
        }

        var meshFilter = hexGo.GetComponent<MeshFilter>();
        meshFilter.mesh = HexMeshUtil.CreatePointyBasedHex(hex.Size, vertices[0]);

        await WaitForWaveJob(vertices, meshFilter, detailLevel);
        hex.SetMesh(meshFilter.mesh);

        return true;
    }

    public async UniTask<bool> SplitMesh(HexViewData hex, int detailLevel) => true;

    private async UniTask<bool> WaitForWaveJob(NativeList<float3> vertices, MeshFilter meshFilter, int detailLevel)
    {
        var mesh = meshFilter.mesh;

        var triangles = new NativeList<int>(mesh.triangles.Length, Allocator.TempJob);
        var uvs = new NativeList<float2>(mesh.uv.Length, Allocator.TempJob);
        var uvs2 = new NativeList<float2>(mesh.uv2.Length, Allocator.TempJob);

        var verticesOut = new NativeList<float3>(Allocator.TempJob);
        var trianglesOut = new NativeList<int>(Allocator.TempJob);
        var uvsOut = new NativeList<float2>(Allocator.TempJob);
        var uvs2Out = new NativeList<float2>(Allocator.TempJob);
        var uvs3Out = new NativeList<float2>(Allocator.TempJob);
        var uvs4Out = new NativeList<float2>(Allocator.TempJob);

        for (var j = 0; j < mesh.triangles.Length; j++) triangles.Add(mesh.triangles[j]);

        for (var j = 0; j < mesh.uv.Length; j++) uvs.Add(mesh.uv[j]);

        for (var j = 0; j < mesh.uv2.Length; j++) uvs2.Add(mesh.uv2[j]);

        var job = new WaveHexBuildJob
        {
            Waves = detailLevel,
            VerticesIn = vertices,
            UvsIn = uvs,
            Uvs2In = uvs2,
            TrianglesOut = trianglesOut,
            VerticesOut = verticesOut,
            UVsOut = uvsOut,
            UVs2Out = uvs2Out,
            UVs3Out = uvs3Out,
            UVs4Out = uvs4Out
        };

        var handler = job.Schedule();
        await handler.ToUniTask(PlayerLoopTiming.Update);
        handler.Complete();

        var newMesh = new Mesh();

        var vertList = new List<Vector3>();
        var triList = new List<int>();
        var uvList = new List<Vector2>();
        var uv2List = new List<Vector2>();
        var uv3List = new List<Vector2>();
        var uv4List = new List<Vector2>();

        for (var j = 0; j < verticesOut.Length; j++) vertList.Add(verticesOut[j]);

        for (var j = 0; j < trianglesOut.Length; j++) triList.Add(trianglesOut[j]);

        for (var j = 0; j < uvsOut.Length; j++) uvList.Add(uvsOut[j]);

        for (var j = 0; j < uvs2Out.Length; j++) uv2List.Add(uvs2Out[j]);

        for (var j = 0; j < uvs3Out.Length; j++) uv3List.Add(uvs3Out[j]);

        for (var j = 0; j < uvs4Out.Length; j++) uv4List.Add(uvs4Out[j]);

        newMesh.SetVertices(vertList);
        newMesh.SetTriangles(triList, 0);

        newMesh.SetUVs(0, uvList);
        newMesh.SetUVs(1, uv2List);
        newMesh.SetUVs(2, uv3List);
        newMesh.SetUVs(3, uv4List);

        newMesh.RecalculateNormals();
        meshFilter.mesh = newMesh;

        vertices.Dispose();
        triangles.Dispose();
        uvs.Dispose();
        uvs2.Dispose();

        verticesOut.Dispose();
        trianglesOut.Dispose();
        uvsOut.Dispose();
        uvs2Out.Dispose();
        uvs3Out.Dispose();
        uvs4Out.Dispose();

        return true;
    }
}