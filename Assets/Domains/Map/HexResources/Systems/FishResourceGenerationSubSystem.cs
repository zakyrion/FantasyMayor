using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Tags;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Configs;
using Domains.Map.HexResources.Data;
using Domains.Map.HexResources.Tags;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Domains.Map.HexResources.Systems
{
    [UsedImplicitly]
    internal sealed class FishResourceGenerationSubSystem : HexResourcesSubSystem
    {
        private readonly World _world;
        private readonly EntitySet _hexSet;
        private readonly EntityMultiMap<HexTypeComponent> _hexesByType;

        public override int Priority => SystemPriorities.SubSystems.HexResourceGeneration.Fish;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Fish;

        public FishResourceGenerationSubSystem(World world) : base(world)
        {
            _world = world;
            _hexSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexLevelComponent>().With<HexTag>()
                .AsSet();
            _hexesByType = world.GetEntities()
                .With<HexTag>()
                .AsMultiMap<HexTypeComponent>();
        }

        public override void Update(GameState state)
        {
            if (!TryGetResourceConfig(out var baseConfig))
                return;

            var config = (FishResourceConfig)baseConfig;
            _hexesByType.TryGetEntities(new HexTypeComponent { Type = HexType.Water }, out var waterEntities);
            var hexEntities = _hexSet.GetEntities();

            if (waterEntities.Length == 0 || hexEntities.Length == 0)
                return;

            var hexCapacity = math.max(1, hexEntities.Length);
            var waterCapacity = math.max(1, waterEntities.Length);
            var levelMap = new NativeParallelHashMap<int2, int>(hexCapacity, Allocator.Temp);
            var waterCoords = new NativeParallelHashSet<int2>(waterCapacity, Allocator.Temp);

            try
            {
                foreach (ref readonly var entity in hexEntities)
                    levelMap.TryAdd(entity.Get<HexIdComponent>().Coords.Value, entity.Get<HexLevelComponent>().Level);

                foreach (ref readonly var entity in waterEntities)
                    waterCoords.Add(entity.Get<HexIdComponent>().Coords.Value);

                var shoreline = BuildShoreline(waterCapacity, ref levelMap, ref waterCoords);
                try
                {
                    if (shoreline.Length == 0)
                        return;

                    var eligible = BuildEligibleZone(waterCapacity, config.DistanceToShore, ref shoreline, ref waterCoords);
                    try
                    {
                        PlaceFish(config, ref eligible);
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

        private NativeList<int2> BuildShoreline(
            int capacity,
            ref NativeParallelHashMap<int2, int> levelMap,
            ref NativeParallelHashSet<int2> waterCoords)
        {
            var shoreline = new NativeList<int2>(capacity, Allocator.Temp);

            foreach (var coord in waterCoords)
            {
                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = coord + AxialMath.NeighborsPointyTop[d];
                    if (levelMap.TryGetValue(neighbor, out var level) && level == 0)
                    {
                        shoreline.Add(coord);
                        break;
                    }
                }
            }

            return shoreline;
        }

        private NativeList<int2> BuildEligibleZone(
            int capacity,
            int distanceToShore,
            ref NativeList<int2> shoreline,
            ref NativeParallelHashSet<int2> waterCoords)
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

                for (var i = 0; i < distanceToShore; i++)
                {
                    nextFrontier.Clear();

                    for (var j = 0; j < frontier.Length; j++)
                    {
                        for (var d = 0; d < AxialMath.NeighborCount; d++)
                        {
                            var neighbor = frontier[j] + AxialMath.NeighborsPointyTop[d];
                            if (waterCoords.Contains(neighbor) && visited.Add(neighbor))
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

        private void PlaceFish(FishResourceConfig config, ref NativeList<int2> eligible)
        {
            if (eligible.Length == 0)
                return;

            var count = Mathf.Clamp(Random.Range(config.Count.x, config.Count.y + 1), 0, eligible.Length);

            if (count <= 0)
                return;

            Shuffle(ref eligible);

            for (var i = 0; i < count; i++)
            {
                var entity = _world.CreateEntity();
                entity.Set(new HexIdFKComponent { Coords = new HexCoord(eligible[i]) });
                entity.Set(new HexResourceComponent { Type = HexResourceType.Fish });
                entity.Set(new HexResourceTag());
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

        public override void Dispose()
        {
            base.Dispose();
            _hexSet.Dispose();
            _hexesByType.Dispose();
        }
    }
}
