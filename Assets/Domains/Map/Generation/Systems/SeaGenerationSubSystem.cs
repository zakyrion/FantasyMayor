using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Domains.Map.Hex.Components;
using Domains.Map.Generation.Components;
using Domains.Map.Generation.Data;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Domains.Map.Generation.Systems
{
    /// <summary>
    ///     Runs sea generation when invoked by <see cref="GenerationSystem" />.
    ///     Generates a single connected sea body seeded from the map edge and biased to
    ///     remain near that edge, with area derived from <see cref="SeaConfigComponent.SizeFraction" />.
    /// </summary>
    [UsedImplicitly]
    internal sealed class SeaGenerationSubSystem : GenerationSubSystem
    {
        private const int ExecutionPriority = 220;
        private const int SeaLevel = -1;
        private const float EdgeWeight = 2f;
        private const float NeighbourWeight = 1f;
        private const float NoiseAmplitude = 0.5f;

        private readonly World _world;
        private readonly EntitySet _hexSet;

        /// <inheritdoc />
        public override int Priority => ExecutionPriority;

        /// <summary>
        ///     Creates a sea generation system bound to the shared ECS world.
        /// </summary>
        /// <param name="world">World used to query terrain config, sea config, and generated hexes.</param>
        public SeaGenerationSubSystem(World world)
        {
            _world = world;
            _hexSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexLevelComponent>()
                .AsSet();
        }

        /// <summary>
        ///     Runs sea generation when the active terrain config requests <see cref="WaterType.Sea" />.
        /// </summary>
        /// <param name="state">Current game state.</param>
        public override void Update(GameState state)
        {
            if (!_world.Has<TerrainGenerationConfigComponent>() || !_world.Has<SeaConfigComponent>())
                return;

            ref readonly var terrainConfig = ref _world.Get<TerrainGenerationConfigComponent>();

            if (terrainConfig.WaterType != WaterType.Sea)
                return;

            ref readonly var config = ref _world.Get<SeaConfigComponent>();

            Generate(in terrainConfig, in config);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
            _hexSet.Dispose();
        }

        /// <summary>
        ///     Applies the generated sea to every hex entity and clears previous sea state elsewhere.
        /// </summary>
        /// <param name="seaCoords">Coordinates that should be marked as water.</param>
        private void ApplySeaLevels(ref NativeParallelHashSet<int2> seaCoords)
        {
            var entities = _hexSet.GetEntities();

            foreach (ref readonly var entity in entities)
            {
                var coord = entity.Get<HexIdComponent>().Coords.Value;
                var level = seaCoords.Contains(coord) ? SeaLevel : 0;
                entity.Set(new HexLevelComponent { Level = level });
            }
        }

        /// <summary>
        ///     Copies all map coordinates from the ECS world into native collections.
        /// </summary>
        /// <param name="mapCoords">Ordered coordinate list to populate.</param>
        /// <param name="mapDomain">Coordinate membership set used during sea growth.</param>
        private void BuildMapCoords(
            ref NativeList<int2> mapCoords,
            ref NativeParallelHashSet<int2> mapDomain)
        {
            var entities = _hexSet.GetEntities();

            foreach (ref readonly var entity in entities)
            {
                var coord = entity.Get<HexIdComponent>().Coords.Value;
                mapDomain.Add(coord);
                mapCoords.Add(coord);
            }
        }

        /// <summary>
        ///     Collects coordinates on the outermost ring of the map, eligible to host the sea seed.
        ///     Falls back to all map coordinates when the outer ring is empty.
        /// </summary>
        /// <param name="mapCoords">All map coordinates.</param>
        /// <param name="mapRadius">Axial distance of the outer ring, derived from config.</param>
        /// <returns>Coordinates eligible to become the sea seed.</returns>
        private NativeList<int2> BuildEdgeDomain(NativeList<int2> mapCoords, int mapRadius)
        {
            var edgeDomain = new NativeList<int2>(mapCoords.Length, Allocator.Temp);

            foreach (var coord in mapCoords)
            {
                if (AxialMath.Distance(coord, int2.zero) == mapRadius)
                    edgeDomain.Add(coord);
            }

            if (edgeDomain.Length > 0)
                return edgeDomain;

            foreach (var coord in mapCoords)
                edgeDomain.Add(coord);

            return edgeDomain;
        }

        /// <summary>
        ///     Counts how many direct neighbors of a coordinate already belong to the sea.
        /// </summary>
        /// <param name="hex">Coordinate to inspect.</param>
        /// <param name="seaCoords">Current sea coordinates.</param>
        /// <returns>Number of neighboring sea hexes.</returns>
        private int CountSeaNeighbors(int2 hex, ref NativeParallelHashSet<int2> seaCoords)
        {
            var count = 0;

            for (var direction = 0; direction < AxialMath.NeighborCount; direction++)
            {
                var neighbor = hex + AxialMath.NeighborsPointyTop[direction];
                if (seaCoords.Contains(neighbor))
                    count++;
            }

            return count;
        }

        /// <summary>
        ///     Adds valid neighboring coordinates to the current growth frontier.
        /// </summary>
        /// <param name="hex">Coordinate whose neighbors should be inspected.</param>
        /// <param name="mapCoords">All valid map coordinates.</param>
        /// <param name="seaCoords">Coordinates already included in the sea.</param>
        /// <param name="frontier">Current frontier candidates.</param>
        private void ExpandFrontier(
            int2 hex,
            ref NativeParallelHashSet<int2> mapCoords,
            ref NativeParallelHashSet<int2> seaCoords,
            ref NativeParallelHashSet<int2> frontier)
        {
            for (var direction = 0; direction < AxialMath.NeighborCount; direction++)
            {
                var neighbor = hex + AxialMath.NeighborsPointyTop[direction];
                if (!mapCoords.Contains(neighbor) || seaCoords.Contains(neighbor))
                    continue;

                frontier.Add(neighbor);
            }
        }

        /// <summary>
        ///     Builds a single sea body from the chosen edge seed and applies it to all matching hex entities.
        /// </summary>
        /// <param name="terrainConfig">Global terrain generation config.</param>
        /// <param name="config">Sea-specific config.</param>
        private void Generate(in TerrainGenerationConfigComponent terrainConfig, in SeaConfigComponent config)
        {
            var entities = _hexSet.GetEntities();
            var mapCapacity = math.max(1, entities.Length);
            var mapCoords = new NativeList<int2>(mapCapacity, Allocator.Temp);
            var mapDomain = new NativeParallelHashSet<int2>(mapCapacity, Allocator.Temp);

            try
            {
                BuildMapCoords(ref mapCoords, ref mapDomain);

                if (mapCoords.Length == 0)
                    return;

                var targetHexCount = Mathf.RoundToInt(config.SizeFraction * mapCoords.Length);
                if (targetHexCount <= 0)
                    return;

                targetHexCount = Mathf.Min(targetHexCount, mapCoords.Length);

                var mapRadius = Mathf.Max(0, terrainConfig.WaveCount - 1);
                var edgeDomain = BuildEdgeDomain(mapCoords, mapRadius);
                try
                {
                    var seed = SelectEdgeSeed(edgeDomain);
                    var seaCoords = GrowSea(seed, mapDomain, targetHexCount, mapRadius);

                    try
                    {
                        ApplySeaLevels(ref seaCoords);
                    }
                    finally
                    {
                        seaCoords.Dispose();
                    }
                }
                finally
                {
                    edgeDomain.Dispose();
                }
            }
            finally
            {
                mapCoords.Dispose();
                mapDomain.Dispose();
            }
        }

        /// <summary>
        ///     Grows a connected sea body from the chosen seed until the requested area is reached.
        /// </summary>
        /// <param name="seed">Edge seed coordinate.</param>
        /// <param name="mapCoords">All valid coordinates present on the current map.</param>
        /// <param name="targetHexCount">Desired sea size in hexes.</param>
        /// <param name="mapRadius">Outer ring radius used to normalise edge-bias scoring.</param>
        /// <returns>Set of coordinates that belong to the generated sea.</returns>
        private NativeParallelHashSet<int2> GrowSea(
            int2 seed,
            NativeParallelHashSet<int2> mapCoords,
            int targetHexCount,
            int mapRadius)
        {
            var lakeCapacity = math.max(1, targetHexCount);
            var frontierCapacity = math.max(1, mapCoords.Count());
            var seaCoords = new NativeParallelHashSet<int2>(lakeCapacity, Allocator.Temp);
            var frontier = new NativeParallelHashSet<int2>(frontierCapacity, Allocator.Temp);

            try
            {
                try
                {
                    seaCoords.Add(seed);
                    ExpandFrontier(seed, ref mapCoords, ref seaCoords, ref frontier);

                    while (seaCoords.Count() < targetHexCount && frontier.Count() > 0)
                    {
                        var nextHex = SelectNextSeaHex(ref frontier, ref seaCoords, mapRadius);
                        frontier.Remove(nextHex);

                        if (!seaCoords.Add(nextHex))
                            continue;

                        ExpandFrontier(nextHex, ref mapCoords, ref seaCoords, ref frontier);
                    }

                    return seaCoords;
                }
                catch
                {
                    seaCoords.Dispose();
                    throw;
                }
            }
            finally
            {
                frontier.Dispose();
            }
        }

        /// <summary>
        ///     Picks a uniform-random seed from the map's outer ring.
        /// </summary>
        /// <param name="edgeDomain">All outer-ring coordinates.</param>
        /// <returns>The chosen seed coordinate.</returns>
        private int2 SelectEdgeSeed(NativeList<int2> edgeDomain)
        {
            return edgeDomain[Random.Range(0, edgeDomain.Length)];
        }

        /// <summary>
        ///     Chooses the next sea hex using edge proximity, connectivity, and a random jitter.
        ///     Distance is normalised to [0, 1] so noise can compete, keeping spread nearly uniform
        ///     while still biasing toward the map edge.
        /// </summary>
        /// <param name="frontier">Current frontier candidates.</param>
        /// <param name="seaCoords">Coordinates already included in the sea.</param>
        /// <param name="mapRadius">Outer ring radius used for distance normalisation.</param>
        /// <returns>Best-scoring frontier coordinate.</returns>
        private int2 SelectNextSeaHex(
            ref NativeParallelHashSet<int2> frontier,
            ref NativeParallelHashSet<int2> seaCoords,
            int mapRadius)
        {
            var hasCandidate = false;
            var bestScore = float.MinValue;
            var bestHex = int2.zero;
            var radiusF = mapRadius > 0 ? (float)mapRadius : 1f;

            foreach (var candidate in frontier)
            {
                var normalizedDistance = AxialMath.Distance(candidate, int2.zero) / radiusF;
                var neighborCount = CountSeaNeighbors(candidate, ref seaCoords);
                var score =
                    normalizedDistance * EdgeWeight +
                    neighborCount * NeighbourWeight +
                    Random.Range(-NoiseAmplitude, NoiseAmplitude);

                if (hasCandidate && score <= bestScore)
                    continue;

                hasCandidate = true;
                bestScore = score;
                bestHex = candidate;
            }

            return bestHex;
        }
    }
}
