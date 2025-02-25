using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DataTypes;
using Unity.Collections;
using UnityEngine;

public class CreateIsolineCommand
{
    public async UniTask<NativeList<Line>> Execute(NativeList<Line> isoline, Rect rect, int resolution)
    {
        var contourmap = new TerrainHeightmap(resolution, Allocator.TempJob);

        var isolineToTextureJob = new IsolineToTextureJob()
        {
            ContourMap = contourmap,
            IsoLine = isoline,
            Rect = rect,
            Resolution = resolution
        };
        
        isolineToTextureJob.Execute();

        return isoline;
    }
}