using Assets.Scripts.MapGenerator.Maps;
using UnityEngine;

namespace Assets.Scripts.MapGenerator.Generators
{
    public class HeightsGenerator : MonoBehaviour, IGenerator
    {
        [Header("Generation Options")]
        public bool Randomize;
        public bool AutoUpdate;

        [Header("Terrain Settings")]
        public FastNoiseLite.NoiseType NoiseType = FastNoiseLite.NoiseType.OpenSimplex2;
        public FastNoiseLite.FractalType FractalType = FastNoiseLite.FractalType.Ridged;
        [Range(1, 100)]
        public int Size;
        [HideInInspector]
        public int Width = 10;
        [HideInInspector]
        public int Height = 10;
        public int Depth = 10;
        public float Scale = 50f;

        [Header("Noise Settings")]
        [Range(3, 9)]
        public int Octaves = 4;
        //[Range(0, 1)]
        public float Lacunarity = 2f;
        [Range(0.3f, 1)]
        public float Persistance = 0.5f;
        public float Seed = 100f;
        public AnimationCurve HeightCurve;

        [Header("Falloff Settings")]
        public bool UseFalloffMap;
        [Range(1, 50)]
        public float FalloffDirection = 3f;
        [Range(1, 60)]
        public float FalloffRange = 3f;

        // Reference to the Terrain component attached to this GameObject
        public Terrain terrain;

        private void OnValidate()
        {
            if (AutoUpdate)
            {
                Generate();
            }
        }

        public void Generate()
        {
            if (Randomize)
            {
                Seed = Random.Range(0f, 9999f);
            }

            if (terrain == null)
            {
                Debug.LogWarning("Terrain component not assigned to the HeightsGenerator script. Please assign it in the Inspector.");
                return;
            }

            // Get the TerrainData for the assigned terrain
            TerrainData terrainData = terrain.terrainData;

            terrainData.size = new Vector3(Width * Size, Depth * Size, Height * Size);

            float[,] falloff = null;
            if (UseFalloffMap)
            {
                falloff = new FalloffMap
                {
                    FalloffDirection = FalloffDirection,
                    FalloffRange = FalloffRange,
                    Size = terrainData.heightmapResolution
                }.Generate();
            }

            float[,] noiseMap = GenerateNoise(falloff, terrainData);
            terrainData.SetHeights(0, 0, noiseMap);
        }

        private float[,] GenerateNoise(float[,] falloffMap = null, TerrainData terrainData = null)
        {
            AnimationCurve heightCurve = new AnimationCurve(HeightCurve.keys);

            float maxLocalNoiseHeight;
            float minLocalNoiseHeight;

            if (terrainData == null)
            {
                Debug.LogWarning("TerrainData not provided for GenerateNoise method.");
                return null;
            }

            float[,] noiseMap = new PerlinMap()
            {
                Size = terrainData.heightmapResolution,
                Octaves = Octaves,
                Scale = Scale,
                Seed = Seed,
                Persistance = Persistance,
                Lacunarity = Lacunarity,
                NoiseType = NoiseType,
                FractalType = FractalType
            }.Generate(out maxLocalNoiseHeight, out minLocalNoiseHeight);

            for (int y = 0; y < terrainData.heightmapResolution; y++)
            {
                for (int x = 0; x < terrainData.heightmapResolution; x++)
                {
                    var lerp = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);

                    if (falloffMap != null)
                    {
                        lerp -= falloffMap[x, y];
                    }

                    if (lerp >= 0)
                    {
                        noiseMap[x, y] = heightCurve.Evaluate(lerp);
                    }
                    else
                    {
                        noiseMap[x, y] = 0;
                    }
                }
            }

            return noiseMap;
        }
    }
}
