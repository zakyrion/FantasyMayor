using DataTypes;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct IsolineToTextureJob : IJob
{
    [ReadOnly] public NativeList<Line> IsoLine;
    [ReadOnly] public Rect Rect;
    public TerrainHeightmap ContourMap;
    public int Resolution;

    public void Execute()
    {
        for (var i = 0; i < IsoLine.Length; i++)
        {
            var startPoint = IsoLine[0];

            var pointOnMap = math.remap(Rect.min, Rect.max, new float2(0, 0), Resolution,
                startPoint.A.xz);

            var nextPointOnMap = math.remap(Rect.min, Rect.max, new float2(0, 0), Resolution,
                startPoint.B.xz);

            pointOnMap = math.clamp(pointOnMap, new float2(0, 0), Resolution);
            nextPointOnMap = math.clamp(nextPointOnMap, new float2(0, 0), Resolution);

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
                ContourMap[new int2(x0, y0)] = 1;
                if (x0 == x1 && y0 == y1) break;

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
        }
    }
}