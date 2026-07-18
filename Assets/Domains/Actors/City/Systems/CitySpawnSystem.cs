using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using Domains.Actors.City.Components;
using Domains.Actors.Components;
using Domains.Kernel.Data;
using Domains.Economy.Resource.Helpers;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Domains.Actors.City.Tags;

namespace Domains.Actors.City.Systems
{
    // One-shot world-init stage: seeds the City id allocator, creates the City actor row, and seeds its
    // inventory loadout from CityConfigComponent (ResourceTypes the author omits start at 0). Open-Closed:
    // a new actor kind adds its own spawn stage, this one never changes.
    [UsedImplicitly]
    internal sealed class CitySpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private readonly EntityStore _world;

        public int Priority => SystemPriorities.WorldInit.CitySpawn;

        public CitySpawnSystem(EntityStore world)
        {
            _world = world;
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            // Idempotent one-shot: an existing allocator means the City was already created
            // (or restored by a future load flow) — never spawn a duplicate on pipeline re-entry.
            if (_world.HasWorldComponent<CityIdAllocatorComponent>())
                return UniTask.CompletedTask;

            if (!_world.HasWorldComponent<CityConfigComponent>())
                throw new InvalidOperationException(
                    "CitySpawnSystem: CityConfigComponent missing — CityConfigLoaderSystem must run at ConfigLoadStep first.");

            var config = _world.GetWorldComponent<CityConfigComponent>();

            _world.SetWorldComponent(new CityIdAllocatorComponent { Next = 1 });

            // Take the next id, advance the allocator, create the row (PK + discriminator).
            var cityId = _world.GetWorldComponent<CityIdAllocatorComponent>().Next;
            _world.SetWorldComponent(new CityIdAllocatorComponent { Next = cityId + 1 });

            var cityIdComponent = new CityIdComponent { Value = cityId };
            var city = _world.CreateEntity();
            city.AddComponent(cityIdComponent);
            city.AddTag<CityTag>();
            city.AddComponent(new ActorTypeComponent { Type = ActorType.City });

            ResourceLoadoutSpawner.SpawnLoadout<CityIdFKComponent, CityResourceTag>(
                _world, new CityIdFKComponent { Value = cityId }, config.Resources);

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
