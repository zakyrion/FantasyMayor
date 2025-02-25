using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct CreateHeightMaskForRegionJob : IJob
{
    private const int FILL_PIXEL_VAL = 1;
    public int Resolution;
    public NativeArray<SpotSegment> BorderLine;
    public float2 PointInside;
    public TerrainHeightmap BinaryMask;

    public Rect Rect;
    public float ClampValue;
    private float2 _resolution;

    public void Execute()
    {
        _resolution = new int2(Resolution, Resolution);

        AddBorders();
        Fill();
        DistanceTransform();
    }

    private void DistanceTransform()
    {
        var summedTable = BinaryMask.ToSummedTable(Allocator.Temp);
        var maxDist = Resolution / 2;

        for (var y = 0; y < Resolution; y++)
        {
            for (var x = 0; x < Resolution; x++)
            {
                var key = new int2(x, y);

                if (BinaryMask[key] > 0)
                {
                    var step = summedTable.BinarySearch(key, maxDist, 0, FILL_PIXEL_VAL);
                    BinaryMask[key] = step;
                }
            }
        }

        BinaryMask.Normalize(maxClamp: ClampValue);
    }

    private void Fill()
    {
        // Convert float3 RectCenterPosition to int2
        var pointInside = math.remap(Rect.min, Rect.max, new float2(), _resolution, PointInside);
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
                BinaryMask[currentPosition] = FILL_PIXEL_VAL;

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
        if (BinaryMask.ContainsKey(position) && BinaryMask[position] >= FILL_PIXEL_VAL)
        {
            return false;
        }

        return position.x >= 0 && position.x < Resolution &&
               position.y >= 0 && position.y < Resolution;
    }

    private void AddBorders()
    {
        var startPoint = BorderLine[0];

        for (var i = 0; i < BorderLine.Length; i++)
        {
            var nextPoint = BorderLine[(i + 1) % BorderLine.Length];

            var pointOnMap = math.remap(Rect.min, Rect.max, new float2(0, 0), _resolution,
                startPoint.Position.xz);

            var nextPointOnMap = math.remap(Rect.min, Rect.max, new float2(0, 0), _resolution,
                nextPoint.Position.xz);

            pointOnMap = math.clamp(pointOnMap, new float2(0, 0), _resolution);
            nextPointOnMap = math.clamp(nextPointOnMap, new float2(0, 0), _resolution);

            var x0 = (int) math.floor(pointOnMap.x);
            var y0 = (int) math.floor(pointOnMap.y);
            var x1 = (int) math.floor(nextPointOnMap.x);
            var y1 = (int) math.floor(nextPointOnMap.y);

            var dx = math.abs(x1 - x0);
            var dy = math.abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                BinaryMask[new int2(x0, y0)] = FILL_PIXEL_VAL;
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }

            startPoint = nextPoint;
        }
    }
}