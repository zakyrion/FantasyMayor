using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

public class MapsCombineCommand
{
    /// <summary>
    ///     Multiply mapA on mapB.
    ///     max value is restricted by max value of specific pixel on both maps
    ///     could be normalized
    /// </summary>
    /// <param name="mapA"></param>
    /// <param name="mapB"></param>
    /// <param name="normalize"></param>
    /// <returns></returns>
    public Texture2D MultiplyMaps(Texture2D mapA, Texture2D mapB, bool normalize = false)
    {
        var texture = new Texture2D(mapA.width, mapA.height, TextureFormat.RFloat, false);
        var colors = new Color[mapA.width * mapA.height];

        var pixelsA = mapA.GetPixels();
        var pixelsB = mapB.GetPixels();

        for (var i = 0; i < pixelsA.Length; i++)
        {
            var colorA = pixelsA[i];
            var colorB = pixelsB[i];

            var color = new Color(colorA.r * colorB.r, 0, 0, 1);
            colors[i] = color;
        }

        if (normalize)
        {
            colors = Normalize(colors);
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    private Color[] Normalize(Color[] colors)
    {
        var max = colors.Max(c => c.r);
        var min = colors.Min(c => c.r);

        for (var i = 0; i < colors.Length; i++)
        {
            var height = math.remap(min, max, 0, 1, colors[i].r);
            colors[i] = new Color(height, 0, 0, 1);
        }

        return colors;
    }

    /// <summary>
    ///     Just sum two maps together.
    ///     Could be normalized
    /// </summary>
    /// <param name="mapA"></param>
    /// <param name="mapB"></param>
    /// <param name="normalize"></param>
    /// <returns></returns>
    public Texture2D AddMaps(Texture2D mapA, Texture2D mapB, bool normalize = false)
    {
        var texture = new Texture2D(mapA.width, mapA.height, TextureFormat.RFloat, false);
        var colors = new Color[mapA.width * mapA.height];

        var pixelsA = mapA.GetPixels();
        var pixelsB = mapB.GetPixels();

        for (var i = 0; i < pixelsA.Length; i++)
        {
            var colorA = pixelsA[i];
            var colorB = pixelsB[i];

            var color = new Color(colorA.r + colorB.r, 0, 0, 1);
            colors[i] = color;
        }

        if (normalize)
        {
            colors = Normalize(colors);
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    /// <summary>
    /// Add two texture maps together.
    /// </summary>
    /// <param name="mapA">The first texture map.</param>
    /// <param name="mapB">The second texture map.</param>
    /// <param name="maxValueOfB">The maximum value of the second map.</param>
    /// <param name="ignoreEmpty">Flag indicating whether to ignore empty pixels in the first map.</param>
    /// <param name="normalized">Flag indicating whether to normalize the result.</param>
    /// <returns>The resulting texture map.</returns>
    public Texture2D AddMaps(Texture2D mapA, Texture2D mapB, float maxValueOfB = 1f, bool ignoreEmpty = true,
        bool normalized = false)
    {
        var texture = new Texture2D(mapA.width, mapA.height, TextureFormat.RFloat, false);
        var colors = new Color[mapA.width * mapA.height];

        var pixelsA = mapA.GetPixels();
        var pixelsB = mapB.GetPixels();

        for (var i = 0; i < pixelsA.Length; i++)
        {
            var colorA = pixelsA[i];
            var colorB = pixelsB[i];

            var color = new Color(colorA.r + (ignoreEmpty && colorA.r > 0f ? colorB.r * maxValueOfB : 0f), 0, 0, 1);
            colors[i] = color;
        }

        if (normalized)
        {
            colors = Normalize(colors);
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    /// <summary>
    ///     lerp mapA and mapB used on mapBlend as a blend factor.
    ///     Could be normalized
    /// </summary>
    /// <param name="mapA"></param>
    /// <param name="mapB"></param>
    /// <param name="mapBlend"></param>
    /// <param name="normalize"></param>
    /// <returns></returns>
    public Texture2D BlendMaps(Texture2D mapA, Texture2D mapB, Texture2D mapBlend, bool normalize = false)
    {
        var texture = new Texture2D(mapA.width, mapA.height, TextureFormat.RFloat, false);
        var colors = new Color[mapA.width * mapA.height];

        var pixelsA = mapA.GetPixels();
        var pixelsB = mapB.GetPixels();
        var pixelsBlend = mapBlend.GetPixels();

        for (var i = 0; i < pixelsA.Length; i++)
        {
            var colorA = pixelsA[i];
            var colorB = pixelsB[i];
            var blended = math.lerp(colorA.r, colorB.r, pixelsBlend[i].r);

            var color = new Color(blended, 0, 0, 1);
            colors[i] = color;
        }

        if (normalize)
        {
            colors = Normalize(colors);
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    /// <summary>
    ///     lerp mapA and mapB used on mapBlend as a blend factor and multiply result on blend factor.
    ///     Could be normalized
    /// </summary>
    /// <param name="mapA"></param>
    /// <param name="mapB"></param>
    /// <param name="mapBlend"></param>
    /// <param name="normalize"></param>
    /// <returns></returns>
    public Texture2D BlendMapsWithRestriction(Texture2D mapA, Texture2D mapB, Texture2D mapBlend,
        bool normalize = false)
    {
        var texture = new Texture2D(mapA.width, mapA.height, TextureFormat.RFloat, false);
        var colors = new Color[mapA.width * mapA.height];

        var pixelsA = mapA.GetPixels();
        var pixelsB = mapB.GetPixels();
        var pixelsBlend = mapBlend.GetPixels();

        for (var i = 0; i < pixelsA.Length; i++)
        {
            var colorA = pixelsA[i];
            var colorB = pixelsB[i];
            var blended = math.lerp(colorA.r, colorB.r, pixelsBlend[i].r) * pixelsBlend[i].r;

            var color = new Color(blended, 0, 0, 1);
            colors[i] = color;
        }

        if (normalize)
        {
            colors = Normalize(colors);
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    /// <summary>
    ///     Sum mapA to mapB multiply cos blend factor.
    ///     Could be normalized
    /// </summary>
    /// <param name="mapA"></param>
    /// <param name="mapB"></param>
    /// <param name="blendFactor"></param>
    /// <returns></returns>
    public Texture2D BlendCosMaps(Texture2D mapA, Texture2D mapB, float blendFactor, bool normalize = false)
    {
        var texture = new Texture2D(mapA.width, mapA.height, TextureFormat.RFloat, false);
        var colors = new Color[mapA.width * mapA.height];

        var pixelsA = mapA.GetPixels();
        var pixelsB = mapB.GetPixels();

        for (var i = 0; i < pixelsA.Length; i++)
        {
            var colorA = pixelsA[i];
            var colorB = pixelsB[i];

            var blend = 0.5f * (1 - math.cos(blendFactor * Mathf.PI));
            var mergedR = colorA.r * (1 - blend) + colorB.r * blend;

            var color = new Color(mergedR, 0, 0, 1);
            colors[i] = color;
        }

        if (normalize)
        {
            colors = Normalize(colors);
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }

    /// <summary>
    ///     Combines two input textures by taking the maximum value of each corresponding pixel channel.
    /// </summary>
    /// <param name="mapA">The first input Texture2D.</param>
    /// <param name="mapB">The second input Texture2D.</param>
    /// <param name="normalize">Determine whether to normalize the resulting texture. Default is false.</param>
    /// <returns>Returns a new Texture2D with the maximum value of each corresponding pixel channel from the input textures.</returns>
    public Texture2D Max(Texture2D mapA, Texture2D mapB, bool normalize = false)
    {
        var texture = new Texture2D(mapA.width, mapA.height, TextureFormat.RFloat, false);
        var colors = new Color[mapA.width * mapA.height];

        var pixelsA = mapA.GetPixels();
        var pixelsB = mapB.GetPixels();

        for (var i = 0; i < pixelsA.Length; i++)
        {
            var colorA = pixelsA[i];
            var colorB = pixelsB[i];

            var color = new Color(math.max(colorA.r, colorB.r), 0, 0, 1);
            colors[i] = color;
        }

        if (normalize)
        {
            colors = Normalize(colors);
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }


    /// <summary>
    ///     Decorates a map with a decoration map using specified ranges.
    /// </summary>
    /// <param name="map">The map texture to be decorated.</param>
    /// <param name="decorationMap">The decoration map texture.</param>
    /// <param name="minRange">The minimum range for decoration.</param>
    /// <param name="maxRange">The maximum range for decoration.</param>
    /// <returns>The decorated map as a new Texture2D object.</returns>
    public UniTask<Texture2D> DecorateMap(Texture2D map, Texture2D decorationMap, float2 minRange, float2 maxRange)
    {
        var texture = new Texture2D(map.width, map.height, TextureFormat.RFloat, false);
        var colors = new Color[map.width * map.height];

        var pixels = map.GetPixels();
        var decorationPixels = decorationMap.GetPixels();

        var min = 0f;
        var max = 1f;

        for (var i = 0; i < pixels.Length; i++)
        {
            var color = pixels[i];
            var decorationColor = decorationPixels[i];

            var range = (float2) math.lerp(minRange, maxRange, (color.r - .3) / .7);

            var delta = math.lerp(range.x, range.y, decorationColor.r);

            var decorated = color.r + delta;

            colors[i] = new Color(decorated, 0, 0, 1);

            min = math.min(min, decorated);
            max = math.max(max, decorated);

            //TODO change decoration range based on current value of map, highest value should have highest range
        }

        if (min < 0 || max > 1)
        {
            for (var i = 0; i < pixels.Length; i++)
            {
                colors[i] = new Color(math.remap(min, max, 0, 1, colors[i].r), 0, 0, 1);
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return UniTask.FromResult(texture);
    }

    public Texture2D SubstractMaps(Texture2D mapA, Texture2D mapB)
    {
        var texture = new Texture2D(mapA.width, mapA.height, TextureFormat.RFloat, false);
        var colors = new Color[mapA.width * mapA.height];

        var pixelsA = mapA.GetPixels();
        var pixelsB = mapB.GetPixels();

        var max = 0f;

        for (var i = 0; i < pixelsA.Length; i++)
        {
            max = math.max(max, pixelsA[i].r);
        }

        for (var i = 0; i < pixelsA.Length; i++)
        {
            var colorA = pixelsA[i];
            var colorB = pixelsB[i];

            var color = new Color((colorA.r - colorB.r) / max, 0, 0, 1);
            colors[i] = color;
        }

        texture.SetPixels(colors);
        texture.Apply();

        return texture;
    }
}