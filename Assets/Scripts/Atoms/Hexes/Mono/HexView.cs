using System;
using Sirenix.OdinInspector;
using UniRx;
using UnityEngine;
using Zenject;

public class HexView : DisposedMono
{
    [SerializeField] private IntReactiveProperty _level;

    private IDisposable[] _disposable;
    private HexViewData _hexData;

    [Inject] private IHexesAPI _hexesAPI;
    [SerializeField] [ReadOnly] private HexId _hexId;

    public void Init(HexViewData hexData)
    {
        _hexData = hexData;
        _hexId = hexData.HexId;

        AddDisposable(hexData.Mesh.Subscribe(ApplyMesh));
        AddDisposable(hexData.Texture.Subscribe(ApplyTexture));
        AddDisposable(_level.Skip(1).Subscribe(ApplyLevel));
    }

    private void ApplyMesh(Mesh mesh)
    {
        var meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }

    private void ApplyTexture(Texture texture)
    {
        var meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.mainTexture = texture;
    }

    private void ApplyLevel(int level)
    {
        _hexesAPI.SetHexLevel(_hexData.HexId, level);
    }
}