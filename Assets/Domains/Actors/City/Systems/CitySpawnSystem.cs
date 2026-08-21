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

namespace Domains.Actors.City.Systems
{
    // One-shot world-init stage: seeds the City id allocator, creates the City actor row, and seeds its
    // inventory loadout from CityConfigComponent (ResourceTypes the author omits start at 0). Open-Closed:
    // a new actor kind adds its own spawn stage, this one never changes.
    [UsedImplicitly]
    internal sealed class CitySpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private readonly EntityStorages _storages;
        private readonly Archetype _cityArchetype;

        public int Priority => SystemPriorities.WorldInit.CitySpawn;

        public CitySpawnSystem(EntityStorages storages)
        {
            _storages = storages;
            _cityArchetype = ActorsArchetypes.City(storages.World);
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            // Idempotent one-shot: an existing allocator means the City was already created
            // (or restored by a future load flow) — never spawn a duplicate on pipeline re-entry.
            if (_storages.Singletons.Has<CityIdAllocatorComponent>())
                return UniTask.CompletedTask;

            if (!_storages.Singletons.Has<CityConfigComponent>())
                throw new InvalidOperationException(
                    "CitySpawnSystem: CityConfigComponent missing — CityConfigLoaderSystem must run at ConfigLoadStep first.");

            var config = _storages.Singletons.Get<CityConfigComponent>();

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
