using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

namespace Atoms.TerrainGeneration
{
    public static class MapSpawnSettings
    {
        public const float HEX_SIZE = 0.75f;
        public const int SUB_HEX_COUNT = 5;
        public static readonly uint Seed = 1234;
        private static Random _random;

        static MapSpawnSettings()
        {
            _random = new Random(Seed);
        }

        public static bool DropDice(int chance)
        {
            return _random.NextInt(100) <= chance;
        }

        public static bool DropDice(float chance)
        {
            return _random.NextFloat() <= chance;
        }

        public static (int2, int2) GetRandomHexPairPositions(int wave)
        {
            wave--;
            if (wave == 0)
                return (new int2(0, 0), new int2(0, 0)); // Center hex

            var directions = new NativeArray<int2>(6, Allocator.Temp);
            InitSubHexCollections(directions);
            // Randomly choose one of the six sides of the hexagonal ring
            var side = _random.NextInt(0, 6);

            // Start at a corner of the hex ring
            var positionOne = directions[side] * wave;

            var stepsOne = _random.NextInt(0, wave);
            // Randomly choose a position along that side
            for (var i = 0; i < stepsOne; i++)
                positionOne += directions[(side + 2) % 6]; // Move along the side

            var sideTwo = side + _random.NextInt(-2, 2) - 2;
            sideTwo = sideTwo < 0 ? 5 + sideTwo : sideTwo > 5 ? sideTwo % 6 : sideTwo;

            var positionTwo = directions[sideTwo] * wave;

            var stepsTwo = _random.NextInt(0, wave);
            while (math.abs(stepsTwo - stepsOne) > 1)
                stepsTwo = _random.NextInt(0, wave);

            // Randomly choose a position along that side
            for (var i = 0; i < stepsTwo; i++)
                positionTwo += directions[(sideTwo + 2) % 6]; // Move along the side

            directions.Dispose();
            return (positionOne, positionTwo);
        }

        /// <summary>
        ///     Generates a random position on a hexagonal grid based on the given wave value.
        /// </summary>
        /// <param name="wave">The wave value used to determine the size of the hexagonal ring.</param>
        /// <returns>A randomly generated position on the hexagonal grid.</returns>
        public static int2 GetRandomHexPosition(int wave)
        {
            wave--;
            if (wave == 0)
                return new int2(0, 0); // Center hex

            var directions = new NativeArray<int2>(6, Allocator.Temp);
            InitSubHexCollections(directions);
            // Randomly choose one of the six sides of the hexagonal ring
            var side = UnityEngine.Random.Range(0, 6);

            // Start at a corner of the hex ring
            var position = directions[side] * wave;

            var steps = _random.NextInt(0, wave);
            // Randomly choose a position along that side
            for (var i = 0; i < steps; i++)
                position += directions[(side + 2) % 6]; // Move along the side

            directions.Dispose();
            return position;
        }

        public static int HexagonCount(int n)
        {
            return 1 + 6 * (n * (n + 1) / 2);
        }

        public static int HexCountInRing(int n)
        {
            return 6 * n;
        }

        public static List<int2> HexesInRing(int n)
        {
            var hexes = new List<int2>();

            // Start at the top right hexagon of the ring
            var x = n;
            var y = -n;

            for (var side = 0; side < 6; side++)
            {
                for (var i = 0; i < n; i++)
                {
                    hexes.Add(new int2(x, y));

                    // Move to the next hexagon on this side of the ring
                    switch (side)
                    {
                        case 0:
                            y++;
                            break; // Move down-right
                        case 1:
                            x--;
                            break; // Move down-left
                        case 2:
                            x--;
                            y++;
                            break; // Move left
                        case 3:
                            y--;
                            break; // Move up-left
                        case 4:
                            x++;
                            break; // Move up-right
                        case 5:
                            x++;
                            y--;
                            break; // Move right
                    }
                }
            }

            return hexes;
        }

        public static void InitSubHexCollections(NativeArray<int2> subHexNeighbours)
        {
            subHexNeighbours[0] = new int2(0, -1);
            subHexNeighbours[1] = new int2(1, -1);
            subHexNeighbours[2] = new int2(1, 0);
            subHexNeighbours[3] = new int2(0, 1);
            subHexNeighbours[4] = new int2(-1, 1);
            subHexNeighbours[5] = new int2(-1, 0);
        }

        public static float NextFloat()
        {
            return _random.NextFloat();
        }
    }
}
