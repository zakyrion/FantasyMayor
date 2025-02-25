using Assets.Scripts.MapGenerator.Generators;
using Assets.Scripts.MapGenerator.Maps;
using Unity.Collections;
using UnityEngine;

public class HeightMapsGenerator : MonoBehaviour
{
    [Header("Terrain Settings")] public FastNoiseLite.NoiseType NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;

    public FastNoiseLite.FractalType FractalType = FastNoiseLite.FractalType.Ridged;

    public float Scale = 50f;

    [Header("Noise Settings")] [Range(3, 9)]
    public int Octaves = 4;

    [Range(0, 1)] public float Lacunarity = 2f;

    [Range(0.3f, 1)] public float Persistance = 0.5f;
    public AnimationCurve HeightCurve;

    [Header("Falloff Settings")] public bool UseFalloffMap;

    [Range(1, 50)] public float FalloffDirection = 3f;

    [Range(1, 60)] public float FalloffRange = 3f;

    private int _resolution;
    private float _seed = 100f;
    
    //TODO: inject data layers here

    private void ApplySettings(HeightsGeneratorSettings settings)
    {
        NoiseType = settings.NoiseType;
        FractalType = settings.FractalType;
        Scale = settings.Scale;
        Octaves = settings.Octaves;
        Lacunarity = settings.Lacunarity;
        Persistance = settings.Persistance;
        HeightCurve = settings.HeightCurve;
        UseFalloffMap = settings.UseFalloffMap;
        FalloffDirection = settings.FalloffDirection;
        FalloffRange = settings.FalloffRange;
    }

    public TerrainHeightmap Generate(HeightsGeneratorSettings settings, uint seed, int resolution)
    {
        ApplySettings(settings);
        _resolution = resolution;
        _seed = seed;

        float[,] falloff = null;
        if (UseFalloffMap)
        {
            falloff = new FalloffMap
            {
                FalloffDirection = FalloffDirection,
                FalloffRange = FalloffRange,
                Size = _resolution
            }.Generate();
        }

        return GenerateNoise(falloff);
    }

    public TerrainHeightmap Generate(HeightsGeneratorSettings settings, TerrainHeightmap falloff, int seed)
    {
        ApplySettings(settings);
        _resolution = falloff.Resolution;
        _seed = seed;

        return GenerateNoise(falloff);
    }

    private TerrainHeightmap GenerateNoise(float[,] falloffMap = null, Allocator allocator = Allocator.TempJob)
    {
        var heightCurve = new AnimationCurve(HeightCurve.keys);

        float maxLocalNoiseHeight;
        float minLocalNoiseHeight;

        var noiseMap = new PerlinMap
        {
            Size = _resolution,
            Octaves = Octaves,
            Scale = Scale,
            Seed = _seed,
            Persistance = Persistance,
            Lacunarity = Lacunarity,
            NoiseType = NoiseType,
            FractalType = FractalType
        }.Generate(out maxLocalNoiseHeight, out minLocalNoiseHeight, allocator);

        for (var y = 0; y < _resolution; y++)
        {
            for (var x = 0; x < _resolution; x++)
            {
                var lerp = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);

                if (falloffMap != null) lerp -= falloffMap[x, y];

                if (lerp >= 0)
                    noiseMap[x, y] = heightCurve.Evaluate(lerp);
                else
                    noiseMap[x, y] = 0;
            }
        }

        return noiseMap;
    }

    private TerrainHeightmap GenerateNoise(TerrainHeightmap falloffMap, Allocator allocator = Allocator.TempJob)
    {
        var heightCurve = new AnimationCurve(HeightCurve.keys);

        float maxLocalNoiseHeight;
        float minLocalNoiseHeight;

        var noiseMap = new PerlinMap
        {
            Size = _resolution,
            Octaves = Octaves,
            Scale = Scale,
            Seed = _seed,
            Persistance = Persistance,
            Lacunarity = Lacunarity,
            NoiseType = NoiseType,
            FractalType = FractalType
        }.Generate(out maxLocalNoiseHeight, out minLocalNoiseHeight, allocator);

        for (var y = 0; y < _resolution; y++)
        {
            for (var x = 0; x < _resolution; x++)
            {
                var lerp = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);

                lerp -= falloffMap[x, y];

                if (lerp >= 0)
                    noiseMap[x, y] = heightCurve.Evaluate(lerp);
                else
                    noiseMap[x, y] = 0;
            }
        }

        return noiseMap;
    }
}