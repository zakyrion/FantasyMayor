using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using Domains.Actors.Archetypes;
using Domains.Actors.City.Components;
using Domains.Actors.City.Tags;
using Domains.Actors.Components;
using Domains.Kernel.Data;
using Domains.Economy.Resource.Helpers;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Domains.Actors.City.Configs;

namespace Domains.Actors.City.Systems
{
    // Map-creation stage: seeds the City id allocator, creates the City actor row, and seeds its
    // inventory loadout from CityConfig (ResourceTypes the author omits start at 0). Open-Closed:
    // a new actor kind adds its own spawn stage, this one never changes.
    [UsedImplicitly]
    internal sealed class CitySpawnSystem : IPipelineStageSystem
    {
        private readonly EntityStorages _storages;
        private readonly Archetype _cityArchetype;

        public AppState AppState { get; }

        public int Priority => SystemPriorities.WorldInit.CitySpawn;

        public CitySpawnSystem(AppState appState, EntityStorages storages)
        {
            AppState = appState;
            _storages = storages;
            _cityArchetype = ActorsArchetypes.City(storages.World);
        }

        public UniTask Execute(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            // Idempotent one-shot: an existing allocator means the City was already created
            // (or restored by a future load flow) — never spawn a duplicate on pipeline re-entry.
            if (_storages.Singletons.Has<CityIdAllocatorComponent>())
                return UniTask.CompletedTask;

            var config = _storages.Get<CityConfig>();

            _storages.Singletons.Set(new CityIdAllocatorComponent { Next = 1 });

            // Take the next id, advance the allocator, create the row (PK + discriminator).
            var cityId = _storages.Singletons.Get<CityIdAllocatorComponent>().Next;
            _storages.Singletons.Set(new CityIdAllocatorComponent { Next = cityId + 1 });

            var cityIdComponent = new CityIdComponent { Value = cityId };
            var city = _cityArchetype.CreateEntity();
            city.AddComponent(cityIdComponent);
            city.AddComponent(new ActorTypeComponent { Type = ActorType.City });

            ResourceLoadoutSpawner.SpawnLoadout<CityIdFKComponent, CityResourceTag>(
                _storages.World, new CityIdFKComponent { Value = cityId }, config.Resources);

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
