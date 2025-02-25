using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public struct SquareNativeArray : IDisposable
{
    public int Resolution;
    public NativeArray<float> Heightmap;
    private readonly int _length;

    public SquareNativeArray(int resolution, Allocator allocator)
    {
        Resolution = resolution;
        Heightmap = new NativeArray<float>(resolution * resolution, allocator);
        _length = Heightmap.Length;
    }

    public float this[int x, int y]
    {
        get
        {
            if (x < 0 || x >= Resolution || y < 0 || y >= Resolution)
            {
                return 1;
            }

            var lineIndex = y * Resolution + x;

            return Heightmap[lineIndex];
        }
        set
        {
            if (x < 0 || x >= Resolution || y < 0 || y >= Resolution)
            {
                return;
            }

            Heightmap[y * Resolution + x] = value;
        }
    }

    public float this[int2 index]
    {
        get
        {
            if (index.x < 0 || index.x >= Resolution || index.y < 0 || index.y >= Resolution)
            {
                return 1;
            }

            var lineIndex = index.y * Resolution + index.x;

            return Heightmap[lineIndex];
        }
        set
        {
            if (index.x < 0 || index.x >= Resolution || index.y < 0 || index.y >= Resolution)
            {
                return;
            }

            var lineIndex = index.y * Resolution + index.x;
            if (lineIndex < _length)
            {
                Heightmap[index.y * Resolution + index.x] = value;
            }
        }
    }

    public void Dispose()
    {
        Heightmap.Dispose();
    }

    public bool ContainsKey(int2 index) => index.x >= 0 && index.x < Resolution && index.y >= 0 && index.y < Resolution;
}