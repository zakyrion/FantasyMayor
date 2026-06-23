using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actors.City.Components;
using Domains.Actors.City.Tags;
using Domains.Actors.Mayor.Components;
using Domains.Actors.Mayor.Tags;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Actors.Systems
{
    // One-shot world-init stage: seeds the id allocators and creates the City and Mayor actor entities.
    [UsedImplicitly]
    internal sealed class ActorsSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        // After the terrain stages (100..800); actors are terrain-independent, so the exact value is cosmetic.
        private const int ExecutionPriority = 900;

        private readonly World _world;

        public int Priority => ExecutionPriority;

        public ActorsSpawnSystem(World world)
        {
            _world = world;
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            // Idempotent one-shot: an existing allocator means the actors were already created
            // (or restored by a future load flow) — never spawn duplicates on pipeline re-entry.
            if (_world.Has<CityIdAllocatorComponent>())
                return UniTask.CompletedTask;

            _world.Set(new CityIdAllocatorComponent { Next = 1 });
            _world.Set(new MayorIdAllocatorComponent { Next = 1 });

            // City: take the next id, advance the allocator, create the row (PK + discriminator).
            var cityId = _world.Get<CityIdAllocatorComponent>().Next;
            _world.Set(new CityIdAllocatorComponent { Next = cityId + 1 });
            var city = _world.CreateEntity();
            city.Set(new CityIdComponent { Value = cityId });
            city.Set(new CityTag());

            // Mayor: single actor (id stays 1); same allocate-then-advance shape for symmetry.
            var mayorId = _world.Get<MayorIdAllocatorComponent>().Next;
            _world.Set(new MayorIdAllocatorComponent { Next = mayorId + 1 });
            var mayor = _world.CreateEntity();
            mayor.Set(new MayorIdComponent { Value = mayorId });
            mayor.Set(new MayorTag());

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
