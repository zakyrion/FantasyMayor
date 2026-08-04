using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Configs;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Domains.Map.HexResources.Systems
{
    [UsedImplicitly]
    internal sealed class ClayResourceGenerationSubSystem : HexResourcesSubSystem
    {
        private const int LandLevel = 0;
        private readonly ComponentIndex<HexTypeComponent, HexType> _hexesByType;
        private readonly Archetype _hexSet;
        private readonly Archetype _hexResourceArchetype;

        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.SubSystems.HexResourceGeneration.Clay;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Clay;

        public ClayResourceGenerationSubSystem(EntityStore world) : base(world)
        {
            _world = world;
            _hexSet = MapArchetypes.Hex(world);
            _hexesByType = world.ComponentIndex<HexTypeComponent, HexType>();
            _hexResourceArchetype = MapArchetypes.HexResource(world);
        }

        public override void Update(GameState state)
        {
            if (!TryGetResourceConfig(out var baseConfig))
                return;

            var config = (ClayResourceConfig)baseConfig;
            var hexEntities = _hexSet.Entities;
            var waterEntities = _hexesByType[HexType.Water];

            if (hexEntities.Count == 0 || waterEntities.Count == 0)
                return;

            var hexCapacity = math.max(1, hexEntities.Count);
            var waterCapacity = math.max(1, waterEntities.Count);
            var levelMap = new NativeParallelHashMap<int2, int>(hexCapacity, Allocator.Temp);
            var waterCoords = new NativeParallelHashSet<int2>(waterCapacity, Allocator.Temp);

            try
            {
                foreach (var entity in hexEntities)
                    levelMap.TryAdd(entity.GetComponent<HexIdComponent>().Coords.Value, entity.GetComponent<HexLevelComponent>().Level);

                foreach (var entity in waterEntities)
                    waterCoords.Add(entity.GetComponent<HexIdComponent>().Coords.Value);

                var shoreline = BuildShoreline(hexCapacity, ref levelMap, ref waterCoords);
                try
                {
                    if (shoreline.Length == 0)
                        return;

                    var eligible = BuildEligibleZone(hexCapacity, config.DistanceToWater, ref shoreline, ref levelMap);
                    try
                    {
                        PlaceClay(config, ref eligible);
                    }
                    finally
                    {
                        eligible.Dispose();
                    }
                }
                finally
                {
                    shoreline.Dispose();
                }
            }
            finally
            {
                levelMap.Dispose();
                waterCoords.Dispose();
            }
        }

        private NativeList<int2> BuildEligibleZone(
            int capacity,
            int distanceToWater,
            ref NativeList<int2> shoreline,
            ref NativeParallelHashMap<int2, int> levelMap)
        {
            var visited = new NativeParallelHashSet<int2>(capacity, Allocator.Temp);
            var frontier = new NativeList<int2>(capacity, Allocator.Temp);
            var nextFrontier = new NativeList<int2>(capacity, Allocator.Temp);

            try
            {
                for (var i = 0; i < shoreline.Length; i++)
                {
                    if (visited.Add(shoreline[i]))
                        frontier.Add(shoreline[i]);
                }

                for (var i = 0; i < distanceToWater; i++)
                {
                    nextFrontier.Clear();

                    for (var j = 0; j < frontier.Length; j++)
                    {
                        for (var d = 0; d < AxialMath.NeighborCount; d++)
                        {
                            var neighbor = frontier[j] + AxialMath.NeighborsPointyTop[d];
                            if (levelMap.TryGetValue(neighbor, out var level) &&
                                level == LandLevel &&
                                visited.Add(neighbor))
                                nextFrontier.Add(neighbor);
                        }
                    }

                    (frontier, nextFrontier) = (nextFrontier, frontier);

                    if (frontier.Length == 0)
                        break;
                }

                var eligible = new NativeList<int2>(visited.Count(), Allocator.Temp);
                foreach (var coord in visited)
                    eligible.Add(coord);

                return eligible;
            }
            finally
            {
                visited.Dispose();
                frontier.Dispose();
                nextFrontier.Dispose();
            }
        }

        private NativeList<int2> BuildShoreline(
            int capacity,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> waterCoords)
        {
            var shoreline = new NativeList<int2>(capacity, Allocator.Temp);

            foreach (var pair in levelMap)
            {
                if (pair.Value != LandLevel)
                    continue;

                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    if (waterCoords.Contains(pair.Key + AxialMath.NeighborsPointyTop[d]))
                    {
                        shoreline.Add(pair.Key);
                        break;
                    }
                }
            }

            return shoreline;
        }

        private void PlaceClay(ClayResourceConfig config, ref NativeList<int2> eligible)
        {
            if (eligible.Length == 0)
                return;

            var count = Mathf.Clamp(Random.Range(config.Count.x, config.Count.y + 1), 0, eligible.Length);

            if (count <= 0)
                return;

            Shuffle(ref eligible);

            for (var i = 0; i < count; i++)
            {
                var entity = _hexResourceArchetype.CreateEntity();
                entity.AddComponent(new HexIdFKComponent { Coords = new HexCoord(eligible[i]) });
                entity.AddComponent(new HexResourceComponent { Type = HexResourceType.Clay });
            }
        }

        private void Shuffle(ref NativeList<int2> list)
        {
            for (var i = list.Length - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
