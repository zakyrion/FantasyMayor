using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public static class TextureExtentions
{
    public static NativeHashMap<int2, float> ToHeightmap(this Texture2D texture, Allocator allocator)
    {
        var map = new NativeHashMap<int2, float>(texture.width * texture.height, allocator);

        for (var x = 0; x < texture.width; x++)
        {
            for (var y = 0; y < texture.height; y++)
            {
                var color = texture.GetPixel(x, y);
                map.Add(new int2(x, y), color.r);
            }
        }

        return map;
    }

    public static Texture2D ToTexture(this NativeHashMap<int2, float> map, int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var colors = new Color[size * size];

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var index = new int2(x, y);
                var value = map[index];
                colors[y * size + x] = new Color(value, 0, 0, 1);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    public static Texture2D ToTexture(this NativeHashMap<int2, byte> map, int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.R8, false);
        var colors = new Color[size * size];

        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var index = new int2(x, y);
                var value = map[index];
                colors[y * size + x] = new Color(value / 256f, 0, 0, 1);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    public static Vector3[] ToVectors(this NativeArray<float3> vertices)
    {
        var result = new Vector3[vertices.Length];

        for (var i = 0; i < vertices.Length; i++)
        {
            result[i] = vertices[i];
        }

        return result;
    }

    public static Vector2[] ToUVs(this NativeArray<float2> uvs)
    {
        var result = new Vector2[uvs.Length];

        for (var i = 0; i < uvs.Length; i++)
        {
            result[i] = uvs[i];
        }

        return result;
    }

    public static NativeArray<float3> ToNativeArray(this Vector3[] vertices, Allocator allocator)
    {
        var result = new NativeArray<float3>(vertices.Length, allocator);

        for (var i = 0; i < vertices.Length; i++)
        {
            result[i] = vertices[i];
        }

        return result;
    }

    public static NativeArray<float2> ToNativeArray(this Vector2[] uvs, Allocator allocator)
    {
        var result = new NativeArray<float2>(uvs.Length, allocator);

        for (var i = 0; i < uvs.Length; i++)
        {
            result[i] = uvs[i];
        }

        return result;
    }
}