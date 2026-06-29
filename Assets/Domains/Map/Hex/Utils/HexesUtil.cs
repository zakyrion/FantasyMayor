using System.Collections.Generic;
using Modules.AxialSystem;
using Unity.Burst;
using Unity.Mathematics;

namespace Domains.Map.Hex.Utils
{
    /// <summary>
    ///     Utility class for working with hexagons.
    /// </summary>
    [BurstCompile]
    public static class HexesUtil
    {
        public static int Count => AxialMath.NeighborCount;

        public static int GetHexCountInWave(int wave)
        {
            return wave == 1 ? 1 : 6 * (wave - 1);
        }

        public static int GetTotalHexCountForWaves(int waveCount)
        {
            return 3 * waveCount * waveCount - 3 * waveCount + 1;
        }

        public static HexCoord IndexToAxialCoords(int index)
        {
            if (index == 0)
                return new HexCoord(0, 0);

            // Знаходимо кільце
            var ring = 1;
            var ringStart = 1;
            while (index >= ringStart + 6 * ring)
            {
                ringStart += 6 * ring;
                ring++;
            }

            // Позиція в кільці
            var posInRing = index - ringStart;

            // Clockwise напрямки: E, SE, SW, W, NW, NE
            int2[] dirs = { new(0, -1), new(-1, 0), new(-1, 1), new(0, 1), new(1, 0), new(1, -1) };

            // Стартуємо з East
            var coords = new HexCoord(ring, 0);

            // Йдемо по сторонах кільця
            for (var side = 0; side < 6; side++)
            {
                for (var step = 0; step < ring && posInRing > 0; step++)
                {
                    coords += dirs[side];
                    posInRing--;
                }
                if (posInRing == 0)
                    break;
            }

            return coords;
        }

        public static bool IsNeighbour(HexCoord a, HexCoord b) => AxialMath.AreNeighbors(a.Value, b.Value);

        /// <summary>
        ///     Returns the neighbor offset at the specified index.
        /// </summary>
        public static int2 Neighbour(int index)
        {
            return index switch
            {
                0 => new int2(0, -1), // right
                5 => new int2(1, -1), // right bottom
                4 => new int2(1, 0), // left bottom
                3 => new int2(0, 1), // left
                2 => new int2(-1, 1), // left top
                1 => new int2(-1, 0), // right top
                _ => default
            };
        }

        /// <summary>
        ///     Returns the position of the neighboring hexagon at the specified index.
        /// </summary>
        public static HexCoord Neighbour(int index, HexCoord hexPosition)
        {
            return hexPosition + Neighbour(index);
        }

        /// <summary>
        ///     Returns all neighbor offsets.
        /// </summary>
        public static IEnumerable<int2> Neighbours()
        {
            for (var i = 0; i <= 5; i++)
                yield return Neighbour(i);
        }

        /// <summary>
        ///     Returns all neighboring hexagons around the given position.
        /// </summary>
        public static IEnumerable<HexCoord> Neighbours(HexCoord position)
        {
            for (var i = 0; i <= 5; i++)
                yield return position + Neighbour(i);
        }

        /// <summary>
        ///     Returns the index of the neighboring hexagon that matches the given offset.
        /// </summary>
        public static int NeighbourToIndex(int2 neighbour)
        {
            for (var i = 0; i < 6; i++)
            {
                if (Neighbour(i).Equals(neighbour))
                    return i;
            }

            return -1;
        }
    }
}
