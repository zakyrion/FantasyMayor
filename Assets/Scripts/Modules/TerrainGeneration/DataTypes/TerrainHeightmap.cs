using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct TerrainHeightmap : IDisposable
{
    public SquareNativeArray Heightmap;

    public TerrainHeightmap(int resolution, Allocator allocator)
    {
        Heightmap = new SquareNativeArray(resolution, allocator);
    }

    public float this[int x, int y]
    {
        get => Heightmap[x, y];
        set => Heightmap[x, y] = value;
    }

    public float this[int2 index]
    {
        get => Heightmap[index];
        set => Heightmap[index] = value;
    }

    public int Resolution => Heightmap.Resolution;

    public void Dispose()
    {
        Heightmap.Dispose();
    }

    public Texture2D ToTexture()
    {
        var resolution = Heightmap.Resolution;
        var texture = new Texture2D(resolution, resolution, TextureFormat.RFloat, false);
        var colors = new Color[resolution * resolution];

        for (var x = 0; x < resolution; x++)
        {
            for (var y = 0; y < resolution; y++)
            {
                var index = y * resolution + x;
                var value = Heightmap[x, y];
                colors[index] = new Color(value, 0, 0, 1);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    public static TerrainHeightmap FromTexture2D(Texture2D texture, Allocator allocator)
    {
        var heightmap = new TerrainHeightmap(texture.width, allocator);
        for (var x = 0; x < texture.width; x++)
        {
            for (var y = 0; y < texture.height; y++)
            {
                var color = texture.GetPixel(x, y);
                heightmap[x, y] = color.r;
            }
        }

        return heightmap;
    }

    public bool TryGetValue(int2 index, out float value)
    {
        if (ContainsKey(index))
        {
            value = Heightmap[index];
            return true;
        }

        value = 0f;
        return false;
    }

    public void Normalize(float minClamp = 0f, float maxClamp = 1f)
    {
        // Find the maximum and minimum values in the heightmap
        var maxValue = float.MinValue;
        var minValue = float.MaxValue;
        //var koef = 1f / maxClamp;
        var koef = 1f;

        for (var x = 0; x < Heightmap.Resolution; x++)
        {
            for (var y = 0; y < Heightmap.Resolution; y++)
            {
                var value = this[x, y];
                maxValue = Math.Max(maxValue, value);
                minValue = Math.Min(minValue, value);
            }
        }

        if (maxValue - 1 < 0.0001f) return;

        var divide = maxValue - minValue;

        for (var x = 0; x < Heightmap.Resolution; x++)
        {
            for (var y = 0; y < Heightmap.Resolution; y++)
                this[x, y] = math.clamp((this[x, y] - minValue) / divide, minClamp, maxClamp) * koef;
        }
    }

    public bool ContainsKey(int2 index) => Heightmap.ContainsKey(index);
    public SummedTable ToSummedTable(Allocator allocator) => new(Heightmap, allocator);

    public static TerrainHeightmap Multiply(TerrainHeightmap a, TerrainHeightmap b, Allocator allocator)
    {
        var result = new TerrainHeightmap(a.Heightmap.Resolution, allocator);
        for (var x = 0; x < a.Heightmap.Resolution; x++)
        {
            for (var y = 0; y < a.Heightmap.Resolution; y++) result[x, y] = a[x, y] * b[x, y];
        }

        return result;
    }
}