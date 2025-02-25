using System;
using Unity.Collections;
using Unity.Mathematics;

public struct SummedTable : IDisposable
{
    public SquareNativeArray Table;

    public SummedTable(SquareNativeArray from, Allocator allocator)
    {
        Table = new SquareNativeArray(from.Resolution, allocator);

        for (var y = 0; y < from.Resolution; y++)
        {
            for (var x = 0; x < from.Resolution; x++)
            {
                var currentIndex = new int2(x, y);
                var leftSum = x > 0 ? Table[new int2(x - 1, y)] : 0f;
                var topSum = y > 0 ? Table[new int2(x, y - 1)] : 0f;
                var topLeftSum = x > 0 && y > 0 ? Table[new int2(x - 1, y - 1)] : 0f;
                var currentVal = from[currentIndex];
                var cellSum = leftSum + topSum - topLeftSum + currentVal;
                Table[x, y] = cellSum;
            }
        }
    }

    public float this[int x, int y]
    {
        get => Table[x, y];
        set => Table[x, y] = value;
    }

    public float this[int2 index]
    {
        get => Table[index];
        set => Table[index] = value;
    }

    public void Dispose()
    {
        Table.Dispose();
    }

    public bool ContainsKey(int2 index) => Table.ContainsKey(index);

    public float GetSummedAreaTable(int2 p1, int2 p2)
    {
        var a = p1.x > 0 && p1.y > 0 ? this[p1] : 0;
        var b = p1.y > 0 ? this[new int2(p2.x, p1.y)] : 0;
        var c = p1.x > 0 ? this[new int2(p1.x, p2.y)] : 0;
        var d = this[p2];
        return d - c - b + a;
    }

    public float GetAverage(int2 p1, int2 p2)
    {
        var area = (p2.x - p1.x) * (p2.y - p1.y);
        return GetSummedAreaTable(p1, p2) / area;
    }

    public float GetAverageExclude(int2 p1, int2 p2, int2 excludeP1, int2 excludeP2)
    {
        var area = (p2.x - p1.x) * (p2.y - p1.y) - (excludeP2.x - excludeP1.x) * (excludeP2.y - excludeP1.y);
        return (GetSummedAreaTable(p1, p2) - GetSummedAreaTable(excludeP1, excludeP2)) / area;
    }

    public int BinarySearch(int2 center, int maxBound, int minBound, float value)
    {
        var min = minBound + (maxBound - minBound) / 2;
        var max = maxBound;

        var pMin = center - new int2(max, max);
        var pMax = center + new int2(max, max);
        var pMinExclude = center - new int2(min, min);
        var pMaxExclude = center + new int2(min, min);

        var densityBig = GetAverageExclude(pMin, pMax, pMinExclude, pMaxExclude);
        var densityExclude = GetAverage(pMinExclude, pMaxExclude);

        if (value - densityExclude > 0.01f)
        {
            return BinarySearch(center, min, minBound, value);
        }

        if (value - densityBig <= 0.01f)
        {
            return max;
        }

        if (max - min == 1)
        {
            return min;
        }

        return BinarySearch(center, max, min, value);
    }

    public int LinearSearch(int2 center, int smoothRadius, float value)
    {
        
        for (var radius = smoothRadius; radius > 0; radius--)
        {
            var minPoint = center - new int2(radius, radius);
            var maxPoint = center + new int2(radius, radius);

            var averageValue = GetAverage(minPoint, maxPoint);

            if (Math.Abs(averageValue - value) < 0.0001f)
            {
                return radius;
            }
        }

        return -1;
    }
}