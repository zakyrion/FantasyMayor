using DefaultEcs;
using DefaultECSExtensions;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Tags;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Configs;
using Domains.Map.HexResources.Data;
using Domains.Map.HexResources.Tags;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Domains.Map.HexResources.Systems
{
    [UsedImplicitly]
    internal sealed class ForestResourceGenerationSubSystem : HexResourcesSubSystem
    {
        private const int ExecutionPriority = 100;
        private const float ForestNeighborWeight = 2f;
        private const float WindBonusWeight = 1.5f;
        private readonly EntityMultiMap<HexTypeComponent> _hexesByType;
        private readonly EntitySet _hexSet;

        private readonly World _world;

        public override int Priority => ExecutionPriority;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Forest;

        public ForestResourceGenerationSubSystem(World world) : base(world)
        {
            _world = world;
            _hexSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexLevelComponent>()
                .AsSet();
            _hexesByType = world.GetEntities()
                .With<HexTag>()
                .AsMultiMap<HexTypeComponent>();
        }

        public override void Update(GameState state)
        {
            if (!TryGetResourceConfig(out var baseConfig))
            {
                Debug.Log("[ForestResourceGenerationSubSystem] Config not found — skipping.");
                return;
            }

            var config = (ForestResourceConfig)baseConfig;
            var hexEntities = _hexSet.GetEntities();
            _hexesByType.TryGetEntities(new HexTypeComponent { Type = HexType.Water }, out var waterEntities);

            Debug.Log($"[ForestResourceGenerationSubSystem] Hex entities: {hexEntities.Length}, water entities: {waterEntities.Length}.");

            if (hexEntities.Length == 0)
            {
                Debug.Log("[ForestResourceGenerationSubSystem] No hex entities — skipping.");
                return;
            }

            var hexCapacity = math.max(1, hexEntities.Length);
            var waterCapacity = math.max(1, waterEntities.Length);
            var levelMap = new NativeParallelHashMap<int2, int>(hexCapacity, Allocator.Temp);
            var waterCoords = new NativeParallelHashSet<int2>(waterCapacity, Allocator.Temp);
            var availableList = new NativeList<int2>(hexCapacity, Allocator.Temp);

            try
            {
                foreach (ref readonly var entity in hexEntities)
                    levelMap.TryAdd(entity.Get<HexIdComponent>().Coords.Value, entity.Get<HexLevelComponent>().Level);

                foreach (ref readonly var entity in waterEntities)
                    waterCoords.Add(entity.Get<HexIdComponent>().Coords.Value);

                foreach (var pair in levelMap)
                {
                    if (!waterCoords.Contains(pair.Key))
                        availableList.Add(pair.Key);
                }

                if (availableList.Length == 0)
                    return;

                var zoneCount = Random.Range(config.ZoneCount.x, config.ZoneCount.y + 1);
                var windDir = Random.Range(0, AxialMath.NeighborCount);
                Debug.Log($"[ForestResourceGenerationSubSystem] Generating {zoneCount} zones, wind dir: {windDir}.");
                var forestCoords = new NativeParallelHashSet<int2>(hexCapacity, Allocator.Temp);

                try
                {
                    for (var z = 0; z < zoneCount; z++)
                    {
                        var zoneSize = Random.Range(config.ZoneSize.x, config.ZoneSize.y + 1);
                        GrowZone(zoneSize, windDir, ref levelMap, ref waterCoords,
                            ref forestCoords, ref availableList);
                    }

                    Debug.Log($"[ForestResourceGenerationSubSystem] Created {forestCoords.Count()} forest entities.");
                }
                finally
                {
                    forestCoords.Dispose();
                }
            }
            finally
            {
                levelMap.Dispose();
                waterCoords.Dispose();
                availableList.Dispose();
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _hexSet.Dispose();
            _hexesByType.Dispose();
        }

        private void AddEligibleNeighbors(
            int2 hex,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> waterCoords,
            ref NativeParallelHashSet<int2> forestCoords,
            ref NativeList<int2> frontier,
            ref NativeParallelHashSet<int2> frontierSet)
        {
            if (!levelMap.TryGetValue(hex, out var hexLevel))
                return;

            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                var neighbor = hex + AxialMath.NeighborsPointyTop[d];

                if (waterCoords.Contains(neighbor) || forestCoords.Contains(neighbor) || frontierSet.Contains(neighbor))
                    continue;

                if (!levelMap.TryGetValue(neighbor, out var neighborLevel))
                    continue;

                if (math.abs(hexLevel - neighborLevel) > 1)
                    continue;

                frontier.Add(neighbor);
                frontierSet.Add(neighbor);
            }
        }

        private float ComputeScore(
            int2 hex,
            ref NativeParallelHashSet<int2> forestCoords,
            int windDir)
        {
            var score = 1f;

            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                var neighbor = hex + AxialMath.NeighborsPointyTop[d];

                if (!forestCoords.Contains(neighbor))
                    continue;

                score += ForestNeighborWeight;

                if (d == windDir)
                    score += WindBonusWeight;
            }

            return score;
        }

        private void GrowZone(
            int zoneSize,
            int windDir,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> waterCoords,
            ref NativeParallelHashSet<int2> forestCoords,
            ref NativeList<int2> availableList)
        {
            var startHex = int2.zero;
            var startFound = false;

            for (var attempt = 0; attempt < availableList.Length; attempt++)
            {
                var candidate = availableList[Random.Range(0, availableList.Length)];
                if (forestCoords.Contains(candidate))
                    continue;

                startHex = candidate;
                startFound = true;
                break;
            }

            if (!startFound)
                return;

            var capacity = math.max(1, zoneSize * AxialMath.NeighborCount);
            var zoneSet = new NativeParallelHashSet<int2>(zoneSize, Allocator.Temp);
            var frontier = new NativeList<int2>(capacity, Allocator.Temp);
            var frontierSet = new NativeParallelHashSet<int2>(capacity, Allocator.Temp);
            var tempScores = new NativeList<float>(capacity, Allocator.Temp);

            try
            {
                zoneSet.Add(startHex);
                forestCoords.Add(startHex);
                AddEligibleNeighbors(startHex, ref levelMap, ref waterCoords,
                    ref forestCoords, ref frontier, ref frontierSet);

                while (zoneSet.Count() < zoneSize && frontier.Length > 0)
                {
                    var pickedIdx = WeightedRandomPick(ref frontier, ref forestCoords, windDir, ref tempScores);
                    var pickedHex = frontier[pickedIdx];

                    frontier[pickedIdx] = frontier[^1];
                    frontier.RemoveAt(frontier.Length - 1);
                    frontierSet.Remove(pickedHex);

                    zoneSet.Add(pickedHex);
                    forestCoords.Add(pickedHex);
                    AddEligibleNeighbors(pickedHex, ref levelMap, ref waterCoords,
                        ref forestCoords, ref frontier, ref frontierSet);
                }

                foreach (var coord in zoneSet)
                {
                    var entity = _world.CreateEntity();
                    entity.Set(new HexIdComponent { Coords = new HexCoord(coord) });
                    entity.Set(new HexResourceComponent { Type = HexResourceType.Forest });
                    entity.Set(new HexResourceTag());
                }
            }
            finally
            {
                zoneSet.Dispose();
                frontier.Dispose();
                frontierSet.Dispose();
                tempScores.Dispose();
            }
        }

        private int WeightedRandomPick(
            ref NativeList<int2> frontier,
            ref NativeParallelHashSet<int2> forestCoords,
            int windDir,
            ref NativeList<float> tempScores)
        {
            tempScores.Clear();
            var totalScore = 0f;

            for (var i = 0; i < frontier.Length; i++)
            {
                var score = ComputeScore(frontier[i], ref forestCoords, windDir);
                tempScores.Add(score);
                totalScore += score;
            }

            var roll = Random.Range(0f, totalScore);
            var cumulative = 0f;

            for (var i = 0; i < tempScores.Length; i++)
            {
                cumulative += tempScores[i];
                if (roll <= cumulative)
                    return i;
            }

            return frontier.Length - 1;
        }
    }
}
