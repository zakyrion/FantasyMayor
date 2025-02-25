using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ApplyRectTextureToVectorFieldCommand
{
    private readonly HexViewDataLayer _hexDataLayer;

    public ApplyRectTextureToVectorFieldCommand(HexViewDataLayer hexDataLayer)
    {
        _hexDataLayer = hexDataLayer;
    }

    public async UniTask<bool> Execute(Rect rect, Texture2D texture, float height)
    {
        var job = new ApplyRectTextureToVectorFieldSimpleJob
        {
            TextureResolution = texture.width,
            Rect = rect,
            Height = height,
            HexVectors = _hexDataLayer.HexVectors,
            HeightMap = texture.ToHeightmap(Allocator.TempJob)
        };

        await job.Schedule();

        return true;
    }
}