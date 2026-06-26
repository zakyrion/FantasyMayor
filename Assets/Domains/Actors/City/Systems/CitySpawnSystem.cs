using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actors.City.Components;
using Domains.Actors.City.Tags;
using Domains.Economy.Resource.Helpers;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Actors.City.Systems
{
    // One-shot world-init stage: seeds the City id allocator, creates the City actor row, and gives it a
    // full inventory loadout (one stack per ResourceType, all at 0 — no City config yet). Open-Closed:
    // a new actor kind adds its own spawn stage, this one never changes.
    [UsedImplicitly]
    internal sealed class CitySpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        // After the terrain stages (100..800); actors are terrain-independent, so the exact value is cosmetic.
        private const int ExecutionPriority = 900;

        private readonly World _world;

        public int Priority => ExecutionPriority;

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

            _world.Set(new CityIdAllocatorComponent { Next = 1 });

            // Take the next id, advance the allocator, create the row (PK + discriminator).
            var cityId = _world.Get<CityIdAllocatorComponent>().Next;
            _world.Set(new CityIdAllocatorComponent { Next = cityId + 1 });

            var cityIdComponent = new CityIdComponent { Value = cityId };
            var city = _world.CreateEntity();
            city.Set(cityIdComponent);
            city.Set(new CityTag());

            ResourceLoadoutSpawner.SpawnLoadout(_world, cityIdComponent);

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
