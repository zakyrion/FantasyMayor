using Assets.Scripts.MapGenerator.Abstract;
using Assets.Scripts.MapGenerator.Generators;
using Unity.Collections;
using UnityEngine;

namespace Assets.Scripts.MapGenerator.Maps
{
    public class PerlinMap : IMap
    {
        public int Size { get; set; }

        public int Octaves { get; set; }
        public float Scale { get; set; }
        public float Seed { get; set; }
        public float Persistance { get; set; }
        public float Lacunarity { get; set; }
        public FastNoiseLite.FractalType FractalType;
        public FastNoiseLite.NoiseType NoiseType;

        public void SetSize(int width, int height)
        {
            // Set the size of the noise map based on the larger of width and height.
            Size = Mathf.Max(width, height);
        }

        public float[,] Generate()
        {
            // Generate the noise map using the default noise settings.
            return GenerateNoiseMap(out _, out _);
        }

        public float[,] Generate(out float maxLocalNoiseHeight, out float minLocalNoiseHeight)
        {
            // Generate the noise map and get the maximum and minimum noise heights.
            return GenerateNoiseMap(out maxLocalNoiseHeight, out minLocalNoiseHeight);
        }
        
        public TerrainHeightmap Generate(out float maxLocalNoiseHeight, out float minLocalNoiseHeight, Allocator allocator = Allocator.TempJob)
        {
            // Generate the noise map and get the maximum and minimum noise heights.
            return GenerateNoiseMap(out maxLocalNoiseHeight, out minLocalNoiseHeight, allocator);
        }

        private TerrainHeightmap GenerateNoiseMap(out float maxLocalNoiseHeight, out float minLocalNoiseHeight,Allocator allocator = Allocator.TempJob)
        {
            // Create a 2D array to store the noise map.
            var noiseMap = new TerrainHeightmap(Size, allocator);

            // Create a new FastNoiseLite object to generate the noise.
            FastNoiseLite noise = new FastNoiseLite();

            // Configure the FastNoiseLite object with the provided noise settings.
            noise.SetNoiseType(NoiseType);
            noise.SetFractalType(FractalType);
            noise.SetFrequency(0.05f); // Frequency determines the coarseness of the noise.
            noise.SetSeed(Mathf.FloorToInt(Seed));
            noise.SetFractalOctaves(Octaves);
            noise.SetFractalLacunarity(Lacunarity);
            noise.SetFractalGain(Persistance);

            // Initialize the max and min noise heights to track the range of noise values.
            maxLocalNoiseHeight = float.MinValue;
            minLocalNoiseHeight = float.MaxValue;

            // Calculate the halfSize of the noise map.
            float halfSize = Size / 2f;

            // Generate the noise map for each point (x, y).
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Calculate the sample position in world space.
                    float sampleX = (x - halfSize) / (float)Size * Scale + Seed;
                    float sampleY = (y - halfSize) / (float)Size * Scale + Seed;

                    // Get the noise value at the sample position.
                    float noiseHeight = noise.GetNoise(sampleX, sampleY);

                    // Store the noise value in the noise map.
                    noiseMap[x, y] = noiseHeight;

                    // Update the maximum and minimum noise heights.
                    if (noiseHeight > maxLocalNoiseHeight)
                    {
                        maxLocalNoiseHeight = noiseHeight;
                    }
                    if (noiseHeight < minLocalNoiseHeight)
                    {
                        minLocalNoiseHeight = noiseHeight;
                    }
                }
            }

            // Return the generated noise map.
            return noiseMap;
        }
        
        private float[,] GenerateNoiseMap(out float maxLocalNoiseHeight, out float minLocalNoiseHeight)
        {
            // Create a 2D array to store the noise map.
            float[,] noiseMap = new float[Size, Size];

            // Create a new FastNoiseLite object to generate the noise.
            FastNoiseLite noise = new FastNoiseLite();

            // Configure the FastNoiseLite object with the provided noise settings.
            noise.SetNoiseType(NoiseType);
            noise.SetFractalType(FractalType);
            noise.SetFrequency(0.05f); // Frequency determines the coarseness of the noise.
            noise.SetSeed(Mathf.FloorToInt(Seed));
            noise.SetFractalOctaves(Octaves);
            noise.SetFractalLacunarity(Lacunarity);
            noise.SetFractalGain(Persistance);

            // Initialize the max and min noise heights to track the range of noise values.
            maxLocalNoiseHeight = float.MinValue;
            minLocalNoiseHeight = float.MaxValue;

            // Calculate the halfSize of the noise map.
            float halfSize = Size / 2f;

            // Generate the noise map for each point (x, y).
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Calculate the sample position in world space.
                    float sampleX = (x - halfSize) / (float)Size * Scale + Seed;
                    float sampleY = (y - halfSize) / (float)Size * Scale + Seed;

                    // Get the noise value at the sample position.
                    float noiseHeight = noise.GetNoise(sampleX, sampleY);

                    // Store the noise value in the noise map.
                    noiseMap[x, y] = noiseHeight;

                    // Update the maximum and minimum noise heights.
                    if (noiseHeight > maxLocalNoiseHeight)
                    {
                        maxLocalNoiseHeight = noiseHeight;
                    }
                    if (noiseHeight < minLocalNoiseHeight)
                    {
                        minLocalNoiseHeight = noiseHeight;
                    }
                }
            }

            // Return the generated noise map.
            return noiseMap;
        }
    }
}
