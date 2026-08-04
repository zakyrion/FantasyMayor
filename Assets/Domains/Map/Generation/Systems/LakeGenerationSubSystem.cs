using Friflo.Engine.ECS;
using EcsExtensions;
using JetBrains.Annotations;
using Domains.Map.Hex.Components;
using Domains.Map.Generation.Components;
using Domains.Map.Generation.Data;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using Domains.Map.Hex.Tags;

namespace Domains.Map.Generation.Systems
{
    /// <summary>
    ///     Runs lake generation when invoked by <see cref="GenerationSystem" />.
    ///     Generates a single connected lake near the map centre with area derived from
    ///     <see cref="LakeConfigComponent.SizeFraction" />.
    /// </summary>
    [UsedImplicitly]
    internal sealed class LakeGenerationSubSystem : GenerationSubSystem
    {
        private const float DistanceWeight = 2f;
        private const int LakeLevel = -1;
        private const float NeighbourWeight = 3f;
        private const float NoiseAmplitude = 0.35f;

        private readonly EntityStore _world;
        private readonly ArchetypeQuery _hexSet;

        /// <inheritdoc />
        public override int Priority => SystemPriorities.SubSystems.Generation.Lake;

        /// <summary>
        ///     Creates a lake generation system bound to the shared ECS world.
        /// </summary>
        /// <param name="world">World used to query terrain config, lake config, and generated hexes.</param>
        public LakeGenerationSubSystem(EntityStore world)
        {
            _world = world;
            _hexSet = world.Query<HexIdComponent, HexLevelComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexTag>());
        }

        /// <summary>
        ///     Runs lake generation when the active terrain config requests <see cref="WaterType.Lake" />.
        /// </summary>
        /// <param name="state">Current game state.</param>
        public override void Update(GameState state)
        {
            if (!_world.HasWorldComponent<TerrainGenerationConfigComponent>() || !_world.HasWorldComponent<LakeConfigComponent>())
                return;

            var terrainConfig = _world.GetWorldComponent<TerrainGenerationConfigComponent>();

            if (terrainConfig.WaterType != WaterType.Lake)
                return;

            var config = _world.GetWorldComponent<LakeConfigComponent>();

            Generate(in terrainConfig, in config);
        }

        /// <summary>
        ///     Applies the generated lake to every hex entity and clears previous lake state elsewhere.
        /// </summary>
        /// <param name="lakeCoords">Coordinates that should be marked as water.</param>
        private void ApplyLakeLevels(ref NativeParallelHashSet<int2> lakeCoords)
        {
            var entities = _hexSet.Entities;

            // AddComponent inside Entities enumeration is a structural change (StructuralChangeException) —
            // snapshot (id, level) first, then re-fetch by id to write.
            var levelById = new NativeList<int2>(entities.Count, Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                {
                    var coord = entity.GetComponent<HexIdComponent>().Coords.Value;
                    var level = lakeCoords.Contains(coord) ? LakeLevel : 0;
                    levelById.Add(new int2(entity.Id, level));
                }

                for (var i = 0; i < levelById.Length; i++)
                {
                    _world.TryGetEntityById(levelById[i].x, out var entity);
                    entity.AddComponent(new HexLevelComponent { Level = levelById[i].y });
                }
            }
            finally
            {
                levelById.Dispose();
            }
        }

        /// <summary>
        ///     Copies all map coordinates from the ECS world into native collections.
        /// </summary>
        /// <param name="mapCoords">Ordered coordinate list to populate.</param>
        /// <param name="mapDomain">Coordinate membership set used during lake growth.</param>
        private void BuildMapCoords(
            ref NativeList<int2> mapCoords,
            ref NativeParallelHashSet<int2> mapDomain)
        {
            var entities = _hexSet.Entities;

            foreach (var entity in entities)
            {
                var coord = entity.GetComponent<HexIdComponent>().Coords.Value;
                mapDomain.Add(coord);
                mapCoords.Add(coord);
            }
        }

        /// <summary>
        ///     Collects coordinates allowed for the lake centre.
        ///     The margin constrains only seed placement, not shoreline expansion.
        /// </summary>
        /// <param name="mapCoords">All map coordinates.</param>
        /// <param name="seedRadius">Maximum axial distance from centre at which a seed may be placed.</param>
        /// <returns>Coordinates eligible to become the lake seed.</returns>
        private NativeList<int2> BuildSeedDomain(NativeList<int2> mapCoords, int seedRadius)
        {
            var seedDomain = new NativeList<int2>(mapCoords.Length, Allocator.Temp);

            foreach (var coord in mapCoords)
            {
                if (AxialMath.Distance(coord, int2.zero) <= seedRadius)
                    seedDomain.Add(coord);
            }

            return seedDomain;
        }

        /// <summary>
        ///     Clears lake state from all currently existing hex entities.
        /// </summary>
        private void ClearLakeLevels()
        {
            var entities = _hexSet.Entities;

            // AddComponent inside Entities enumeration is a structural change (StructuralChangeException) —
            // snapshot ids first, then re-fetch by id to write.
            var ids = new NativeList<int>(entities.Count, Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                    ids.Add(entity.Id);

                for (var i = 0; i < ids.Length; i++)
                {
                    _world.TryGetEntityById(ids[i], out var entity);
                    entity.AddComponent(new HexLevelComponent { Level = 0 });
                }
            }
            finally
            {
                ids.Dispose();
            }
        }

        /// <summary>
        ///     Counts how many direct neighbors of a coordinate already belong to the lake.
        /// </summary>
        /// <param name="hex">Coordinate to inspect.</param>
        /// <param name="lakeCoords">Current lake coordinates.</param>
        /// <returns>Number of neighboring lake hexes.</returns>
        private int CountLakeNeighbors(int2 hex, ref NativeParallelHashSet<int2> lakeCoords)
        {
            var count = 0;

            for (var direction = 0; direction < AxialMath.NeighborCount; direction++)
            {
                var neighbor = hex + AxialMath.NeighborsPointyTop[direction];
                if (lakeCoords.Contains(neighbor))
                    count++;
            }

            return count;
        }

        /// <summary>
        ///     Adds valid neighboring coordinates to the current growth frontier.
        /// </summary>
        /// <param name="hex">Coordinate whose neighbors should be inspected.</param>
        /// <param name="mapCoords">All valid map coordinates.</param>
        /// <param name="lakeCoords">Coordinates already included in the lake.</param>
        /// <param name="frontier">Current frontier candidates.</param>
        private void ExpandFrontier(
            int2 hex,
            ref NativeParallelHashSet<int2> mapCoords,
            ref NativeParallelHashSet<int2> lakeCoords,
            ref NativeParallelHashSet<int2> frontier)
        {
            for (var direction = 0; direction < AxialMath.NeighborCount; direction++)
            {
                var neighbor = hex + AxialMath.NeighborsPointyTop[direction];
                if (!mapCoords.Contains(neighbor) || lakeCoords.Contains(neighbor))
                    continue;

                frontier.Add(neighbor);
            }
        }

        /// <summary>
        ///     Builds a single lake using current map coordinates and applies it to all matching hex entities.
        /// </summary>
        /// <param name="terrainConfig">Global terrain generation config.</param>
        /// <param name="config">Lake-specific config.</param>
        private void Generate(in TerrainGenerationConfigComponent terrainConfig, in LakeConfigComponent config)
        {
            var entities = _hexSet.Entities;
            var mapCapacity = math.max(1, entities.Count);
            var mapCoords = new NativeList<int2>(mapCapacity, Allocator.Temp);
            var mapDomain = new NativeParallelHashSet<int2>(mapCapacity, Allocator.Temp);

            try
            {
                BuildMapCoords(ref mapCoords, ref mapDomain);

                if (mapCoords.Length == 0)
                    return;

                var targetHexCount = Mathf.RoundToInt(config.SizeFraction * mapCoords.Length);
                if (targetHexCount <= 0)
                {
                    return;
                }

                targetHexCount = Mathf.Min(targetHexCount, mapCoords.Length);

                var mapRadius = Mathf.Max(0, terrainConfig.WaveCount - 1);
                var seedRadius = Mathf.Max(0, mapRadius - Mathf.Max(0, config.EdgeMarginTiles));
                var seedDomain = BuildSeedDomain(mapCoords, seedRadius);
                try
                {
                    var seed = SelectSeed(seedDomain, seedRadius);
                    var lakeCoords = GrowLake(seed, mapDomain, targetHexCount);

                    try
                    {
                        ApplyLakeLevels(ref lakeCoords);
                    }
                    finally
                    {
                        lakeCoords.Dispose();
                    }
                }
                finally
                {
                    seedDomain.Dispose();
                }
            }
            finally
            {
                mapCoords.Dispose();
                mapDomain.Dispose();
            }
        }

        /// <summary>
        ///     Grows a connected lake from the chosen seed until the requested area is reached.
        /// </summary>
        /// <param name="seed">Seed coordinate placed near the map centre.</param>
        /// <param name="mapCoords">All valid coordinates present on the current map.</param>
        /// <param name="targetHexCount">Desired lake size in hexes.</param>
        /// <returns>Set of coordinates that belong to the generated lake.</returns>
        private NativeParallelHashSet<int2> GrowLake(int2 seed, NativeParallelHashSet<int2> mapCoords, int targetHexCount)
        {
            var lakeCapacity = math.max(1, targetHexCount);
            var frontierCapacity = math.max(1, mapCoords.Count());
            var lakeCoords = new NativeParallelHashSet<int2>(lakeCapacity, Allocator.Temp);
            var frontier = new NativeParallelHashSet<int2>(frontierCapacity, Allocator.Temp);

            try
            {
                try
                {
                    lakeCoords.Add(seed);
                    ExpandFrontier(seed, ref mapCoords, ref lakeCoords, ref frontier);

                    while (lakeCoords.Count() < targetHexCount && frontier.Count() > 0)
                    {
                        var nextHex = SelectNextLakeHex(ref frontier, ref lakeCoords, seed);
                        frontier.Remove(nextHex);

                        if (!lakeCoords.Add(nextHex))
                            continue;

                        ExpandFrontier(nextHex, ref mapCoords, ref lakeCoords, ref frontier);
                    }

                    return lakeCoords;
                }
                catch
                {
                    lakeCoords.Dispose();
                    throw;
                }
            }
            finally
            {
                frontier.Dispose();
            }
        }

        /// <summary>
        ///     Chooses the next lake hex using connectivity, compactness, and a small random jitter.
        /// </summary>
        /// <param name="frontier">Current frontier candidates.</param>
        /// <param name="lakeCoords">Coordinates already included in the lake.</param>
        /// <param name="seed">Lake seed used as the compactness anchor.</param>
        /// <returns>Best-scoring frontier coordinate.</returns>
        private int2 SelectNextLakeHex(
            ref NativeParallelHashSet<int2> frontier,
            ref NativeParallelHashSet<int2> lakeCoords,
            int2 seed)
        {
            var hasCandidate = false;
            var bestScore = float.MinValue;
            var bestHex = seed;

            foreach (var candidate in frontier)
            {
                var neighborCount = CountLakeNeighbors(candidate, ref lakeCoords);
                var distanceFromSeed = AxialMath.Distance(candidate, seed);
                var score =
                    neighborCount * NeighbourWeight -
                    distanceFromSeed * DistanceWeight +
                    Random.Range(-NoiseAmplitude, NoiseAmplitude);

                if (hasCandidate && score <= bestScore)
                    continue;

                hasCandidate = true;
                bestScore = score;
                bestHex = candidate;
            }

            return bestHex;
        }

        /// <summary>
        ///     Selects a random seed with central coordinates weighted more heavily than peripheral ones.
        /// </summary>
        /// <param name="seedDomain">All coordinates allowed to host the lake centre.</param>
        /// <param name="maxDistance">Maximum axial distance within the seed domain, derived from config.</param>
        /// <returns>The chosen seed coordinate.</returns>
        private int2 SelectSeed(NativeList<int2> seedDomain, int maxDistance)
        {
            var totalWeight = 0;
            foreach (var coord in seedDomain)
            {
                var distance = AxialMath.Distance(coord, int2.zero);
                totalWeight += maxDistance - distance + 1;
            }

            var roll = Random.Range(0, totalWeight);

            foreach (var coord in seedDomain)
            {
                var distance = AxialMath.Distance(coord, int2.zero);
                roll -= maxDistance - distance + 1;
                if (roll < 0)
                    return coord;
            }

            return seedDomain[seedDomain.Length - 1];
        }
    }
}
