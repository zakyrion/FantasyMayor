using Domains.Map.Archetypes;
using Domains.Map.Generation.Components;
using Domains.Map.Hex.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Domains.Map.Generation.Systems
{
    /// <summary>
    ///     Runs mountain generation when invoked by <see cref="GenerationSystem" />.
    ///     Places a blob-shaped mountain body (Level 2) inside a water-safe zone using
    ///     multi-seed BFS: seeds first grow toward each other (centroid pull), then spread
    ///     outward by neighbour cohesion. Foothills (Level 1) are applied in a post-processing pass.
    /// </summary>
    [UsedImplicitly]
    internal sealed class MountainGenerationSubSystem : GenerationSubSystem
    {
        private const int WaterLevel = -1;
        private const int FoothillLevel = 1;
        private const int MountainLevel = 2;
        private const float MergeWeight = 3f;
        private const float CohesionWeight = 1.5f;
        private const float GrowthJitter = 0.5f;

        private readonly EntityStorages _storages;
        private readonly Archetype _hexSet;

        /// <inheritdoc />
        public override int Priority => SystemPriorities.SubSystems.Generation.Mountain;

        /// <summary>
        ///     Creates a mountain generation system bound to the shared ECS world.
        /// </summary>
        /// <param name="storages">Named ECS storages used to query mountain config and generated hexes.</param>
        public MountainGenerationSubSystem(EntityStorages storages)
        {
            _storages = storages;
            _hexSet = MapArchetypes.Hex(storages.World);
        }

        /// <inheritdoc />
        public override void Update(GameState state)
        {
            if (!_storages.World.HasWorldComponent<MountainConfigComponent>())
                return;

            var config = _storages.World.GetWorldComponent<MountainConfigComponent>();

            Generate(in config);
        }

        /// <summary>
        ///     Copies all map coordinates and their current levels from the ECS world.
        /// </summary>
        /// <param name="mapCoords">Ordered coordinate list to populate.</param>
        /// <param name="levelMap">Coordinate-to-level map to populate.</param>
        private void BuildMapState(
            ref NativeList<int2> mapCoords,
            ref NativeParallelHashMap<int2, int> levelMap)
        {
            foreach (var entity in _hexSet.Entities)
            {
                var coord = entity.GetComponent<HexIdComponent>().Coords.Value;
                mapCoords.Add(coord);
                levelMap.TryAdd(coord, entity.GetComponent<HexLevelComponent>().Level);
            }
        }

        /// <summary>
        ///     Extracts water coordinates (Level == <see cref="WaterLevel" />) from the level map.
        /// </summary>
        /// <param name="mapCoords">All map coordinates.</param>
        /// <param name="levelMap">Current coordinate-to-level map.</param>
        /// <param name="waterCoords">Set to populate with water coordinates.</param>
        private void BuildWaterCoords(
            NativeList<int2> mapCoords,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> waterCoords)
        {
            foreach (var coord in mapCoords)
            {
                if (levelMap[coord] == WaterLevel)
                    waterCoords.Add(coord);
            }
        }

        /// <summary>
        ///     Returns true if any of the six direct neighbours of <paramref name="hex" /> is a water hex.
        /// </summary>
        /// <param name="hex">Coordinate to test.</param>
        /// <param name="waterCoords">Set of water coordinates.</param>
        private bool IsAdjacentToWater(int2 hex, ref NativeParallelHashSet<int2> waterCoords)
        {
            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                if (waterCoords.Contains(hex + AxialMath.NeighborsPointyTop[d]))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Builds the set of hexes eligible for mountain placement: Level 0 and not adjacent to water.
        /// </summary>
        /// <param name="mapCoords">All map coordinates.</param>
        /// <param name="levelMap">Current coordinate-to-level map.</param>
        /// <param name="waterCoords">Set of water coordinates.</param>
        /// <param name="safeList">Ordered list of safe coordinates (for seed selection).</param>
        /// <param name="safeDomain">Set of safe coordinates (for frontier membership checks).</param>
        private void BuildSafeZone(
            NativeList<int2> mapCoords,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> waterCoords,
            ref NativeList<int2> safeList,
            ref NativeParallelHashSet<int2> safeDomain)
        {
            foreach (var coord in mapCoords)
            {
                if (levelMap[coord] != 0)
                    continue;

                if (IsAdjacentToWater(coord, ref waterCoords))
                    continue;

                safeList.Add(coord);
                safeDomain.Add(coord);
            }
        }

        /// <summary>
        ///     Selects <paramref name="seedCount" /> seeds from the safe zone.
        ///     The first seed is chosen uniformly at random. Each subsequent seed is picked
        ///     from candidates within <paramref name="maxSeedDistance" /> of the nearest existing seed,
        ///     maximising its minimum distance to already-chosen seeds.
        ///     Falls back to the closest available hex if no candidate satisfies the distance constraint.
        /// </summary>
        /// <param name="safeList">All safe-zone coordinates.</param>
        /// <param name="seedCount">Number of seeds to select.</param>
        /// <param name="maxSeedDistance">Maximum axial distance to the nearest existing seed.</param>
        /// <returns>Selected seed coordinates.</returns>
        private NativeList<int2> SelectSeeds(NativeList<int2> safeList, int seedCount, int maxSeedDistance)
        {
            var seeds = new NativeList<int2>(seedCount, Allocator.Temp);
            seeds.Add(safeList[Random.Range(0, safeList.Length)]);

            for (var i = 1; i < seedCount; i++)
            {
                var bestCandidate = seeds[i - 1];
                var bestMinDist = int.MinValue;
                var fallbackCandidate = seeds[i - 1];
                var fallbackDist = int.MaxValue;

                foreach (var coord in safeList)
                {
                    var minDistToSeeds = int.MaxValue;

                    for (var s = 0; s < seeds.Length; s++)
                    {
                        var d = AxialMath.Distance(coord, seeds[s]);
                        if (d < minDistToSeeds)
                            minDistToSeeds = d;
                    }

                    // Fallback: closest hex to the last seed (ignores distance constraint)
                    var distToLast = AxialMath.Distance(coord, seeds[i - 1]);
                    if (distToLast > 0 && distToLast < fallbackDist)
                    {
                        fallbackDist = distToLast;
                        fallbackCandidate = coord;
                    }

                    if (minDistToSeeds == 0 || minDistToSeeds > maxSeedDistance)
                        continue;

                    // Within constraint: prefer the one furthest from all existing seeds
                    if (minDistToSeeds > bestMinDist)
                    {
                        bestMinDist = minDistToSeeds;
                        bestCandidate = coord;
                    }
                }

                seeds.Add(bestMinDist > int.MinValue ? bestCandidate : fallbackCandidate);
            }

            return seeds;
        }

        /// <summary>
        ///     Adds safe-zone neighbours of <paramref name="hex" /> not yet in the mountain to the frontier.
        /// </summary>
        /// <param name="hex">Coordinate whose neighbours should be inspected.</param>
        /// <param name="safeDomain">All coordinates eligible for mountain placement.</param>
        /// <param name="mountainCoords">Coordinates already included in the mountain.</param>
        /// <param name="frontier">Current frontier candidates.</param>
        private void ExpandFrontier(
            int2 hex,
            ref NativeParallelHashSet<int2> safeDomain,
            ref NativeParallelHashSet<int2> mountainCoords,
            ref NativeParallelHashSet<int2> frontier)
        {
            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                var neighbor = hex + AxialMath.NeighborsPointyTop[d];
                if (!safeDomain.Contains(neighbor) || mountainCoords.Contains(neighbor))
                    continue;

                frontier.Add(neighbor);
            }
        }

        /// <summary>
        ///     Counts how many direct neighbours of <paramref name="hex" /> already belong to the mountain.
        /// </summary>
        /// <param name="hex">Coordinate to inspect.</param>
        /// <param name="mountainCoords">Current mountain coordinates.</param>
        /// <returns>Number of neighbouring mountain hexes.</returns>
        private int CountMountainNeighbors(int2 hex, ref NativeParallelHashSet<int2> mountainCoords)
        {
            var count = 0;

            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                if (mountainCoords.Contains(hex + AxialMath.NeighborsPointyTop[d]))
                    count++;
            }

            return count;
        }

        /// <summary>
        ///     Scores a frontier candidate by centroid proximity (merge pull) and neighbour cohesion
        ///     (outward compactness). No noise functions are used.
        ///     <para>
        ///         While blobs have not merged, candidates closer to the centroid score higher → growth
        ///         toward other seeds. After merging, frontier candidates are equidistant from the centroid
        ///         and neighbour count dominates → compact outward growth.
        ///     </para>
        /// </summary>
        /// <param name="frontier">Current frontier candidates.</param>
        /// <param name="mountainCoords">Coordinates already included in the mountain.</param>
        /// <param name="centroid">Average position of all seeds.</param>
        /// <returns>Best-scoring frontier coordinate.</returns>
        private int2 SelectNextMountainHex(
            ref NativeParallelHashSet<int2> frontier,
            ref NativeParallelHashSet<int2> mountainCoords,
            float2 centroid)
        {
            var hasCandidate = false;
            var bestScore = float.MinValue;
            var bestHex = int2.zero;

            foreach (var candidate in frontier)
            {
                var dist = math.length((float2)candidate - centroid);
                var neighborCount = CountMountainNeighbors(candidate, ref mountainCoords);
                var score =
                    MergeWeight / math.max(1f, dist) +
                    neighborCount * CohesionWeight +
                    Random.Range(-GrowthJitter, GrowthJitter);

                if (hasCandidate && score <= bestScore)
                    continue;

                hasCandidate = true;
                bestScore = score;
                bestHex = candidate;
            }

            return bestHex;
        }

        /// <summary>
        ///     Runs a combined BFS from all seeds, growing toward the shared centroid first, then outward.
        /// </summary>
        /// <param name="seeds">Selected seed coordinates.</param>
        /// <param name="safeDomain">All coordinates eligible for mountain placement.</param>
        /// <param name="targetHexCount">Desired mountain size in hexes.</param>
        /// <returns>Set of coordinates that form the mountain body.</returns>
        private NativeParallelHashSet<int2> GrowMountain(
            NativeList<int2> seeds,
            NativeParallelHashSet<int2> safeDomain,
            int targetHexCount)
        {
            var mountainCapacity = math.max(1, targetHexCount);
            var frontierCapacity = math.max(1, safeDomain.Count());
            var mountainCoords = new NativeParallelHashSet<int2>(mountainCapacity, Allocator.Temp);
            var frontier = new NativeParallelHashSet<int2>(frontierCapacity, Allocator.Temp);

            var centroidSum = float2.zero;
            for (var i = 0; i < seeds.Length; i++)
                centroidSum += (float2)seeds[i];
            var centroid = centroidSum / math.max(1, seeds.Length);

            try
            {
                try
                {
                    for (var i = 0; i < seeds.Length; i++)
                    {
                        mountainCoords.Add(seeds[i]);
                        ExpandFrontier(seeds[i], ref safeDomain, ref mountainCoords, ref frontier);
                    }

                    while (mountainCoords.Count() < targetHexCount && frontier.Count() > 0)
                    {
                        var nextHex = SelectNextMountainHex(ref frontier, ref mountainCoords, centroid);
                        frontier.Remove(nextHex);

                        if (!mountainCoords.Add(nextHex))
                            continue;

                        ExpandFrontier(nextHex, ref safeDomain, ref mountainCoords, ref frontier);
                    }

                    return mountainCoords;
                }
                catch
                {
                    mountainCoords.Dispose();
                    throw;
                }
            }
            finally
            {
                frontier.Dispose();
            }
        }

        /// <summary>
        ///     Counts how many on-map neighbours of <paramref name="hex" /> have the given level.
        /// </summary>
        /// <param name="hex">Coordinate to inspect.</param>
        /// <param name="level">Level value to match.</param>
        /// <param name="levelMap">Current coordinate-to-level map.</param>
        /// <returns>Count of neighbours at the specified level.</returns>
        private int CountNeighborsAtLevel(
            int2 hex,
            int level,
            ref NativeParallelHashMap<int2, int> levelMap)
        {
            var count = 0;

            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                var neighbor = hex + AxialMath.NeighborsPointyTop[d];
                if (levelMap.TryGetValue(neighbor, out var neighborLevel) && neighborLevel == level)
                    count++;
            }

            return count;
        }

        /// <summary>
        ///     Counts how many of the six potential neighbours of <paramref name="hex" /> exist on the map.
        /// </summary>
        /// <param name="hex">Coordinate to inspect.</param>
        /// <param name="levelMap">Current coordinate-to-level map.</param>
        /// <returns>Count of on-map neighbours (0–6).</returns>
        private int CountExistingNeighbors(int2 hex, ref NativeParallelHashMap<int2, int> levelMap)
        {
            var count = 0;

            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                if (levelMap.ContainsKey(hex + AxialMath.NeighborsPointyTop[d]))
                    count++;
            }

            return count;
        }

        /// <summary>
        ///     Updates a level value in the map.
        ///     <see cref="NativeParallelHashMap{TKey,TValue}" /> has no direct setter for existing keys,
        ///     so Remove + TryAdd is used.
        /// </summary>
        private void SetLevel(ref NativeParallelHashMap<int2, int> levelMap, int2 coord, int level)
        {
            levelMap.Remove(coord);
            levelMap.TryAdd(coord, level);
        }

        /// <summary>
        ///     Writes Level 2 to all mountain coordinates, then runs three post-processing passes:
        ///     enclosed rule, mandatory foothills, and general foothills.
        /// </summary>
        /// <param name="mapCoords">All map coordinates.</param>
        /// <param name="mountainCoords">Mountain body coordinates.</param>
        /// <param name="levelMap">Level map — updated in place as rules are applied.</param>
        /// <param name="waterCoords">Water coordinates used for adjacency checks.</param>
        /// <param name="minFoothillNeighbors">Minimum Level 2 neighbour count to qualify as a general foothill.</param>
        /// <param name="minFoothillCount">Minimum number of foothills that must be placed.</param>
        /// <param name="minFoothillDistance">Minimum contour distance between mandatory foothills.</param>
        private void ApplyMountainAndFoothills(
            NativeList<int2> mapCoords,
            ref NativeParallelHashSet<int2> mountainCoords,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> waterCoords,
            int minFoothillNeighbors,
            int minFoothillCount,
            int minFoothillDistance)
        {
            var entities = _hexSet.Entities;

            // AddComponent inside Entities enumeration is a structural change (StructuralChangeException) —
            // snapshot (coord -> id) once, re-fetch by id to write in every pass below (incl. PlaceMandatoryFoothills).
            var idByCoord = new NativeParallelHashMap<int2, int>(math.max(1, entities.Count), Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                    idByCoord.TryAdd(entity.GetComponent<HexIdComponent>().Coords.Value, entity.Id);

                // Pass 1: mountain body
                foreach (var coord in mapCoords)
                {
                    if (!mountainCoords.Contains(coord))
                        continue;

                    _storages.World.TryGetEntityById(idByCoord[coord], out var entity);
                    entity.AddComponent(new HexLevelComponent { Level = MountainLevel });
                    SetLevel(ref levelMap, coord, MountainLevel);
                }

                // Pass 2: enclosed rule
                foreach (var coord in mapCoords)
                {
                    if (levelMap[coord] != 0)
                        continue;

                    var existingNeighbors = CountExistingNeighbors(coord, ref levelMap);
                    var mountainNeighbors = CountNeighborsAtLevel(coord, MountainLevel, ref levelMap);

                    if (existingNeighbors > 0 && mountainNeighbors == existingNeighbors)
                    {
                        _storages.World.TryGetEntityById(idByCoord[coord], out var entity);
                        entity.AddComponent(new HexLevelComponent { Level = MountainLevel });
                        SetLevel(ref levelMap, coord, MountainLevel);
                    }
                }

                // Pass 3: mandatory foothills with contour-distance spacing
                PlaceMandatoryFoothills(mapCoords, ref levelMap, ref waterCoords, ref idByCoord, minFoothillCount, minFoothillDistance);

                // Pass 4: general foothills rule
                foreach (var coord in mapCoords)
                {
                    if (levelMap[coord] != 0)
                        continue;

                    var mountainNeighbors = CountNeighborsAtLevel(coord, MountainLevel, ref levelMap);

                    if (mountainNeighbors >= minFoothillNeighbors &&
                        !IsAdjacentToWater(coord, ref waterCoords))
                    {
                        _storages.World.TryGetEntityById(idByCoord[coord], out var entity);
                        entity.AddComponent(new HexLevelComponent { Level = FoothillLevel });
                        SetLevel(ref levelMap, coord, FoothillLevel);
                    }
                }
            }
            finally
            {
                idByCoord.Dispose();
            }
        }

        /// <summary>
        ///     Collects all Level 2 hexes that lie on the mountain border (have at least one
        ///     neighbour not at Level 2 or off-map).
        /// </summary>
        /// <param name="mapCoords">All map coordinates.</param>
        /// <param name="levelMap">Current coordinate-to-level map.</param>
        /// <param name="contour">Set to populate with contour coordinates.</param>
        private void BuildContour(
            NativeList<int2> mapCoords,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> contour)
        {
            foreach (var coord in mapCoords)
            {
                if (levelMap[coord] != MountainLevel)
                    continue;

                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = coord + AxialMath.NeighborsPointyTop[d];
                    if (!levelMap.TryGetValue(neighbor, out var nLevel) || nLevel != MountainLevel)
                    {
                        contour.Add(coord);
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Collects Level 0 hexes adjacent to the mountain contour that are not adjacent to water.
        ///     Populates parallel lists: <paramref name="candidates" /> and their nearest contour hex
        ///     <paramref name="anchors" />.
        /// </summary>
        /// <param name="mapCoords">All map coordinates.</param>
        /// <param name="levelMap">Current coordinate-to-level map.</param>
        /// <param name="waterCoords">Water coordinates.</param>
        /// <param name="contour">Mountain contour coordinates.</param>
        /// <param name="candidates">Foothill candidate coordinates.</param>
        /// <param name="anchors">Contour anchor for each candidate (parallel with candidates).</param>
        private void BuildFoothillCandidates(
            NativeList<int2> mapCoords,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> waterCoords,
            ref NativeParallelHashSet<int2> contour,
            ref NativeList<int2> candidates,
            ref NativeList<int2> anchors)
        {
            foreach (var coord in mapCoords)
            {
                if (levelMap[coord] != 0)
                    continue;

                if (IsAdjacentToWater(coord, ref waterCoords))
                    continue;

                var anchor = int2.zero;
                var hasAnchor = false;

                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = coord + AxialMath.NeighborsPointyTop[d];
                    if (!contour.Contains(neighbor))
                        continue;

                    anchor = neighbor;
                    hasAnchor = true;
                    break;
                }

                if (!hasAnchor)
                    continue;

                candidates.Add(coord);
                anchors.Add(anchor);
            }
        }

        /// <summary>
        ///     BFS from <paramref name="startAnchor" /> across the contour graph and updates
        ///     <paramref name="contourDist" /> to record the minimum distance from any selected
        ///     mandatory foothill anchor to every contour hex.
        /// </summary>
        /// <param name="startAnchor">Contour hex to start the BFS from.</param>
        /// <param name="contour">All mountain contour hexes.</param>
        /// <param name="contourDist">Running minimum-distance map — updated in place.</param>
        private void UpdateContourDistances(
            int2 startAnchor,
            ref NativeParallelHashSet<int2> contour,
            ref NativeParallelHashMap<int2, int> contourDist)
        {
            var capacity = math.max(1, contour.Count());
            var queue = new NativeList<int2>(capacity, Allocator.Temp);
            var localDist = new NativeParallelHashMap<int2, int>(capacity, Allocator.Temp);

            try
            {
                queue.Add(startAnchor);
                localDist.TryAdd(startAnchor, 0);

                var head = 0;
                while (head < queue.Length)
                {
                    var current = queue[head++];
                    localDist.TryGetValue(current, out var dist);

                    for (var d = 0; d < AxialMath.NeighborCount; d++)
                    {
                        var neighbor = current + AxialMath.NeighborsPointyTop[d];
                        if (!contour.Contains(neighbor) || localDist.ContainsKey(neighbor))
                            continue;

                        localDist.TryAdd(neighbor, dist + 1);
                        queue.Add(neighbor);
                    }
                }

                for (var i = 0; i < queue.Length; i++)
                {
                    var hex = queue[i];
                    localDist.TryGetValue(hex, out var d);

                    if (contourDist.TryGetValue(hex, out var existing))
                    {
                        if (d < existing)
                        {
                            contourDist.Remove(hex);
                            contourDist.TryAdd(hex, d);
                        }
                    }
                    else
                    {
                        contourDist.TryAdd(hex, d);
                    }
                }
            }
            finally
            {
                queue.Dispose();
                localDist.Dispose();
            }
        }

        /// <summary>
        ///     Places at least <paramref name="minCount" /> mandatory foothill hexes around the mountain,
        ///     each separated from all others by at least <paramref name="minDistance" /> steps along
        ///     the mountain contour. The first foothill is chosen randomly; each subsequent one is picked
        ///     by maximising its minimum contour distance to already-placed foothills.
        ///     Stops early if no candidate satisfies the distance constraint.
        /// </summary>
        /// <param name="mapCoords">All map coordinates.</param>
        /// <param name="levelMap">Current coordinate-to-level map — updated in place.</param>
        /// <param name="waterCoords">Water coordinates used for adjacency checks.</param>
        /// <param name="idByCoord">Snapshot of hex entity ids by coordinate — see <see cref="ApplyMountainAndFoothills" />.</param>
        /// <param name="minCount">Minimum number of mandatory foothills to place.</param>
        /// <param name="minDistance">Minimum contour distance between any two mandatory foothills.</param>
        private void PlaceMandatoryFoothills(
            NativeList<int2> mapCoords,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> waterCoords,
            ref NativeParallelHashMap<int2, int> idByCoord,
            int minCount,
            int minDistance)
        {
            if (minCount <= 0)
                return;

            var capacity = math.max(1, mapCoords.Length);
            var contour = new NativeParallelHashSet<int2>(capacity, Allocator.Temp);

            try
            {
                BuildContour(mapCoords, ref levelMap, ref contour);

                if (contour.Count() == 0)
                    return;

                var candidates = new NativeList<int2>(capacity, Allocator.Temp);
                var anchors = new NativeList<int2>(capacity, Allocator.Temp);

                try
                {
                    BuildFoothillCandidates(mapCoords, ref levelMap, ref waterCoords, ref contour,
                        ref candidates, ref anchors);

                    if (candidates.Length == 0)
                        return;

                    var contourDist = new NativeParallelHashMap<int2, int>(contour.Count(), Allocator.Temp);
                    var selected = new NativeParallelHashSet<int2>(minCount, Allocator.Temp);

                    try
                    {
                        // First foothill: random
                        var firstIdx = Random.Range(0, candidates.Length);
                        selected.Add(candidates[firstIdx]);
                        UpdateContourDistances(anchors[firstIdx], ref contour, ref contourDist);

                        // Subsequent foothills: maximise min contour distance to already-placed ones
                        for (var i = 1; i < minCount; i++)
                        {
                            var bestIdx = -1;
                            var bestDist = -1;

                            for (var c = 0; c < candidates.Length; c++)
                            {
                                if (selected.Contains(candidates[c]))
                                    continue;

                                var d = contourDist.TryGetValue(anchors[c], out var dist) ? dist : int.MaxValue;

                                if (d > bestDist)
                                {
                                    bestDist = d;
                                    bestIdx = c;
                                }
                            }

                            if (bestIdx == -1 || bestDist < minDistance)
                                break;

                            selected.Add(candidates[bestIdx]);
                            UpdateContourDistances(anchors[bestIdx], ref contour, ref contourDist);
                        }

                        foreach (var coord in mapCoords)
                        {
                            if (!selected.Contains(coord))
                                continue;

                            _storages.World.TryGetEntityById(idByCoord[coord], out var entity);
                            entity.AddComponent(new HexLevelComponent { Level = FoothillLevel });
                            SetLevel(ref levelMap, coord, FoothillLevel);
                        }
                    }
                    finally
                    {
                        contourDist.Dispose();
                        selected.Dispose();
                    }
                }
                finally
                {
                    candidates.Dispose();
                    anchors.Dispose();
                }
            }
            finally
            {
                contour.Dispose();
            }
        }

        /// <summary>
        ///     Runs the full mountain generation pipeline.
        /// </summary>
        /// <param name="config">Mountain-specific config.</param>
        private void Generate(in MountainConfigComponent config)
        {
            var entities = _hexSet.Entities;
            var mapCapacity = math.max(1, entities.Count);
            var mapCoords = new NativeList<int2>(mapCapacity, Allocator.Temp);
            var levelMap = new NativeParallelHashMap<int2, int>(mapCapacity, Allocator.Temp);

            try
            {
                BuildMapState(ref mapCoords, ref levelMap);

                if (mapCoords.Length == 0)
                    return;

                var waterCoords = new NativeParallelHashSet<int2>(mapCapacity, Allocator.Temp);

                try
                {
                    BuildWaterCoords(mapCoords, ref levelMap, ref waterCoords);

                    var safeList = new NativeList<int2>(mapCapacity, Allocator.Temp);
                    var safeDomain = new NativeParallelHashSet<int2>(mapCapacity, Allocator.Temp);

                    try
                    {
                        BuildSafeZone(mapCoords, ref levelMap, ref waterCoords, ref safeList, ref safeDomain);

                        if (safeList.Length == 0)
                            return;

                        var targetHexCount = Mathf.Min(
                            Mathf.RoundToInt(config.SizeFraction * mapCoords.Length),
                            safeList.Length);

                        if (targetHexCount <= 0)
                            return;

                        var seedCount = Mathf.Max(1, config.SeedCount);
                        var seeds = SelectSeeds(safeList, seedCount, config.MaxSeedDistance);

                        try
                        {
                            var mountainCoords = GrowMountain(seeds, safeDomain, targetHexCount);

                            try
                            {
                                ApplyMountainAndFoothills(mapCoords, ref mountainCoords, ref levelMap, ref waterCoords,
                                    config.MinFoothillNeighbors, config.MinFoothillCount, config.MinFoothillDistance);
                            }
                            finally
                            {
                                mountainCoords.Dispose();
                            }
                        }
                        finally
                        {
                            seeds.Dispose();
                        }
                    }
                    finally
                    {
                        safeList.Dispose();
                        safeDomain.Dispose();
                    }
                }
                finally
                {
                    waterCoords.Dispose();
                }
            }
            finally
            {
                mapCoords.Dispose();
                levelMap.Dispose();
            }
        }
    }
}
