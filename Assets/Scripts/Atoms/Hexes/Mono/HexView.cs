using System;
using Sirenix.OdinInspector;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

public class HexView : DisposedMono
{
    [SerializeField] private IntReactiveProperty _level;

    [SerializeField][ReadOnly] private int2 _position;

    private IDisposable[] _disposable;
    private HexViewData _hexData;

    [Inject] private IHexesAPI _hexesAPI;

    public void Init(HexViewData hexData)
    {
        _hexData = hexData;
        _position = hexData.Position;
        
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
        _hexesAPI.SetHexLevel(_hexData.Position, level);
    }
}