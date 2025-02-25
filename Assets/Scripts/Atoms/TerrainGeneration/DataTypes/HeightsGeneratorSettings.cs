using System;
using Assets.Scripts.MapGenerator.Generators;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class HeightsGeneratorSettings
{
    #region Terrain Settings

    [FormerlySerializedAs("TerrainType")] public SurfaceType _surfaceType;

    [Header("Terrain Settings")] public FastNoiseLite.NoiseType NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;

    public FastNoiseLite.FractalType FractalType = FastNoiseLite.FractalType.Ridged;
    public float Scale = 50f;

    #endregion
    
    #region Noise Settings
    [Header("Noise Settings")] [Range(3, 9)]
    public int Octaves = 4;
    [Range(0, 5)] public float Lacunarity = 0.5f;
    [Range(0.3f, 5)] public float Persistance = 0.5f;
    public AnimationCurve HeightCurve;

    #endregion

    #region Falloff

    [Header("Falloff Settings")] public bool UseFalloffMap;
    [Range(1, 50)] public float FalloffDirection = 3f;
    [Range(1, 60)] public float FalloffRange = 3f;

    #endregion
}