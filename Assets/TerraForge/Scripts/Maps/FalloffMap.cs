using Assets.Scripts.MapGenerator.Abstract;
using UnityEngine;

namespace Assets.Scripts.MapGenerator.Maps
{
    public class FalloffMap : IMap
    {
        // Variables to control the shape of the falloff map
        public float FalloffDirection; // Controls how steep the falloff curve is
        public float FalloffRange;     // Controls the range of the falloff effect
        public int Size;               // The size of the falloff map (width and height)

        // This method is part of the IMap interface, but it's not used here
        // We set the size directly in the properties instead
        public void SetSize(int width, int height)
        {
            Size = Mathf.Max(width, height);
        }

        // Generates the falloff map as a 2D array of floats
        public float[,] Generate()
        {
            // Create a 2D array to store the falloff map
            float[,] map = new float[Size, Size];

            // Loop through each pixel of the map
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    // Convert the pixel coordinates to a range from -1 to 1
                    float x = i / (float)Size * 2 - 1;
                    float y = j / (float)Size * 2 - 1;

                    // Calculate the distance from the center of the map
                    float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                    // Evaluate the falloff function and store the result in the map
                    map[i, j] = Evaluate(value);
                }
            }

            // Return the generated falloff map
            return map;
        }

        // Evaluate the falloff function for a given value
        float Evaluate(float value)
        {
            // Apply a falloff function to the input value
            // The result will be in the range from 0 to 1
            // The falloff function is designed to create a smooth gradient from the center to the edges
            // The FalloffDirection and FalloffRange variables control the shape of the gradient
            return Mathf.Pow(value, FalloffDirection) / (Mathf.Pow(value, FalloffDirection) + Mathf.Pow(FalloffRange - FalloffRange * value, FalloffDirection));
        }
    }
}
