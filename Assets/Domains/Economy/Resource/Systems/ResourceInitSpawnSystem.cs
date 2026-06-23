using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actors.City.Components;
using Domains.Actors.City.Tags;
using Domains.Actors.Mayor.Components;
using Domains.Actors.Mayor.Tags;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Helpers;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Economy.Resource.Systems
{
    // One-shot world-init stage: gives every City and Mayor a full inventory-resource loadout
    // (one stack per ResourceType). The Mayor's starting amounts come from MayorConfig
    // (MayorConfigComponent, loaded at ConfigLoadStep); the City starts every stack at 0 (no City config
    // yet). Runs after ActorsSpawnSystem (priority 900) so the actor rows exist.
    // Noble loadouts are NOT handled here — Nobles emerge during play and get their resources from a
    // separate reactive system (deferred). See Economy/ECONOMY.md.
    [UsedImplicitly]
    internal sealed class ResourceInitSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const int ExecutionPriority = 1000;

        private readonly World _world;

        public int Priority => ExecutionPriority;

        public ResourceInitSpawnSystem(World world)
        {
            _world = world;
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            using var cities = _world.GetEntities().With<CityIdComponent>().With<CityTag>().AsSet();
            var cityEntities = cities.GetEntities();
            if (cityEntities.Length == 0)
                throw new InvalidOperationException(
                    "ResourceInitSpawnSystem: no City actor found — ActorsSpawnSystem must run first.");

            foreach (var city in cityEntities)
                ResourceLoadoutSpawner.SpawnLoadout(_world, city.Get<CityIdComponent>());

            using var mayors = _world.GetEntities().With<MayorIdComponent>().With<MayorTag>().AsSet();
            var mayorEntities = mayors.GetEntities();
            if (mayorEntities.Length == 0)
                throw new InvalidOperationException(
                    "ResourceInitSpawnSystem: no Mayor actor found — ActorsSpawnSystem must run first.");

            if (!_world.Has<MayorConfigComponent>())
                throw new InvalidOperationException(
                    "ResourceInitSpawnSystem: MayorConfigComponent missing — MayorConfigLoaderSystem must run at ConfigLoadStep first.");

            var mayorConfig = _world.Get<MayorConfigComponent>();

            foreach (var mayor in mayorEntities)
                ResourceLoadoutSpawner.SpawnLoadout(_world, mayor.Get<MayorIdComponent>(), mayorConfig.Resources);

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
