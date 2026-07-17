using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
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
        private readonly World _world;

        public int Priority => SystemPriorities.WorldInit.CitySpawn;

        public CitySpawnSystem(World world)
        {
            _world = world;
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            // Idempotent one-shot: an existing allocator means the City was already created
            // (or restored by a future load flow) — never spawn a duplicate on pipeline re-entry.
            if (_world.Has<CityIdAllocatorComponent>())
                return UniTask.CompletedTask;

            if (!_world.Has<CityConfigComponent>())
                throw new InvalidOperationException(
                    "CitySpawnSystem: CityConfigComponent missing — CityConfigLoaderSystem must run at ConfigLoadStep first.");

            var config = _world.Get<CityConfigComponent>();

            _world.Set(new CityIdAllocatorComponent { Next = 1 });

            // Take the next id, advance the allocator, create the row (PK + discriminator).
            var cityId = _world.Get<CityIdAllocatorComponent>().Next;
            _world.Set(new CityIdAllocatorComponent { Next = cityId + 1 });

            var cityIdComponent = new CityIdComponent { Value = cityId };
            var city = _world.CreateEntity();
            city.Set(cityIdComponent);
            city.Set(new CityTag());
            city.Set(new ActorTypeComponent { Type = ActorType.City });

            ResourceLoadoutSpawner.SpawnLoadout<CityIdFKComponent, CityResourceTag>(
                _world, new CityIdFKComponent { Value = cityId }, config.Resources);

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
