using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct CreateLevelsForRegionJob : IJob
{
    public int Resolution;
    public TerrainHeightmap Heightmap;
    public NativeArray<Triangle> Triangles;
    public float2 PointInside;
    public Rect Region;

    public float MaxHeight;

    public void Execute()
    {
        SetTrianglesHeight();
        Fill();
    }

    /// <summary>
    ///     Sets the height for triangles by filling inner space with the maximum height and outer space with 0.
    /// </summary>
    private void SetTrianglesHeight()
    {
        var textureStart = new float2(0, 0);
        var textureEnd = new float2(Resolution, Resolution);

        var regionStart = new float2(Region.xMin, Region.yMin);
        var regionEnd = new float2(Region.xMax, Region.yMax);

        foreach (var triangle in Triangles)
        {
            var subRect = triangle.Rect();

            var rectStart = new float2(subRect.xMin, subRect.yMin);
            var rectEnd = new float2(subRect.xMax, subRect.yMax);

            var pixelStart = math.remap(regionStart, regionEnd, textureStart, textureEnd, rectStart);
            var pixelEnd = math.remap(regionStart, regionEnd, textureStart, textureEnd, rectEnd);

            for (var x = (int) pixelStart.x; x < pixelEnd.x; x++)
            {
                for (var z = (int) pixelStart.y; z < pixelEnd.y; z++)
                {
                    var position = math.remap(textureStart, textureEnd, regionStart, regionEnd, new float2(x, z));

                    if (triangle.Inside(position))
                    {
                        var height = Sigmoid(triangle.GetHeightAtPoint(position));

                        if (height > Heightmap[x, z]) Heightmap[x, z] = height * MaxHeight;
                    }
                }
            }
        }
    }

    private void Fill()
    {
        var resolution = new float2(Resolution, Resolution);
        // Convert float3 RectCenterPosition to int2
        var pointInside = math.remap(Region.min, Region.max, new float2(), resolution, PointInside);
        var pixel = new int2((int) math.floor(pointInside.x), (int) math.floor(pointInside.y));

        // Initialize a queue with the center position
        var queue = new NativeQueue<int2>(Allocator.TempJob);
        queue.Enqueue(pixel);

        // Start filling from the center
        while (queue.Count > 0)
        {
            var currentPosition = queue.Dequeue();

            if (ShouldPixelBeFilled(currentPosition))
            {
                Heightmap[currentPosition] = MaxHeight;

                // Add neighboring pixels to the queue
                queue.Enqueue(new int2(currentPosition.x + 1, currentPosition.y));
                queue.Enqueue(new int2(currentPosition.x - 1, currentPosition.y));
                queue.Enqueue(new int2(currentPosition.x, currentPosition.y + 1));
                queue.Enqueue(new int2(currentPosition.x, currentPosition.y - 1));
            }
        }
    }

    private bool ShouldPixelBeFilled(int2 position)
    {
        // Don't fill if pixel is already filled
        if (Heightmap[position] > 0) return false;

        return position.x >= 0 && position.x < Resolution &&
               position.y >= 0 && position.y < Resolution;
    }


    private float Sigmoid(float x) => 1f / (1f + math.exp(-10 * (x - .5f)));
}