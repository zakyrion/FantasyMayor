using Atoms.Hexes.DataLayer;
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

    public async UniTask<bool> Execute(Rect rect, TerrainHeightmap heightmap, float height)
    {
        var job = new ApplyRectTextureToVectorFieldSimpleJob
        {
            Rect = rect,
            Height = height,
            HexVectors = _hexDataLayer.HexVectors,
            HeightMap = heightmap
        };

        await job.Schedule();

        return true;
    }
}