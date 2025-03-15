using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

/// <summary>
/// Class responsible for generating regions for terrain heightmaps based on specified resolution.
/// </summary>
public class CreateRegionForCommand
{
    private readonly int _resolution;

    public CreateRegionForCommand(int resolution)
    {
        _resolution = resolution;
    }

    public async UniTask<Texture2D> GetTextureHeightmap(Spot spot)
    {
        return await GetHeightmap(spot).ContinueWith(heightmap => heightmap.ToTexture());
    }

    public async UniTask<TerrainHeightmap> GetHeightmap(Spot spot, float clamp = .5f)
    {
        var binaryMask = new TerrainHeightmap(_resolution, Allocator.TempJob);

        var job = new CreateHeightMaskForRegionJob
        {
            PointInside = spot.PointInside.xz,
            Resolution = _resolution,
            BorderLine = spot.BorderLine,
            BinaryMask = binaryMask,
            Rect = spot.Rect,
            ClampValue = clamp
        };

        await job.Schedule();
        //job.Execute();

        return binaryMask;
    }
}