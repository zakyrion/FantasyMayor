using Cysharp.Threading.Tasks;
using Modules.Hexes.DataLayer;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class ApplyVectorForcesCommand
{
    private readonly HexViewDataLayer _hexDataLayer;

    public ApplyVectorForcesCommand(HexViewDataLayer hexDataLayer)
    {
        _hexDataLayer = hexDataLayer;
    }
   
    public async UniTask<TerrainHeightmap> Execute(int resolution, NativeList<CircleEmitter> circleEmitters)
    {
        int totalPixels = resolution * resolution;
        var heightmap = new TerrainHeightmap(resolution, Allocator.TempJob);

        //var job = new ApplyVectorForcesJob
        var job = new ParallelApplyVectorForcesJob
        {
            Resolution = resolution,
            Heightmap = heightmap,
            CircleEmitters = circleEmitters,
            NoiseScale = 0.08f
        };
        
        await job.Schedule(totalPixels, 64);
        heightmap.Normalize();
        
        //job.Execute();

        return heightmap;
    }
}