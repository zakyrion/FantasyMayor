using Cysharp.Threading.Tasks;
using Unity.Collections;
using UnityEngine;

public class ApplyVectorForcesCommand
{
    private readonly HexViewDataLayer _hexDataLayer;

    public ApplyVectorForcesCommand(HexViewDataLayer hexDataLayer)
    {
        _hexDataLayer = hexDataLayer;
    }

    //додати точки позитивних емітерів 
    //додати точки негативних емітерів
    //додати круглі емітери
    //додати еліптичні емітери
    //додати гесагональні емітери
    //позитивні емітери можуть бути тільки всередині гексів
    //негативні можуть бути лише біля кордонів гексів і не дуже великі
    public async UniTask<TerrainHeightmap> Execute(int resolution, NativeList<CircleEmitter> circleEmitters)
    {
        var heightmap = new TerrainHeightmap(resolution, Allocator.TempJob);

        var job = new ApplyVectorForcesJob
        {
            Resolution = resolution,
            Heightmap = heightmap,
            CircleEmitters = circleEmitters,
            NoiseScale = 0.08f
        };
        
        job.Execute();

        return heightmap;
    }
}