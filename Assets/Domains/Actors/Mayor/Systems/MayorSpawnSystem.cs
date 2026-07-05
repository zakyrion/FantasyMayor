using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actors.Components;
using Domains.Kernel.Data;
using Domains.Actors.Mayor.Components;
using Domains.Economy.Resource.Helpers;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Actors.Mayor.Systems
{
    // One-shot world-init stage: seeds the Mayor id allocator, creates the singleton Mayor actor, and seeds
    // its starting state from MayorConfigComponent — the inventory loadout, the per-turn AP restore amount
    // (MayorAPRestoreComponent), and the starting ActionPoint resource stack (the live AP pool, seeded here
    // because the AP-restore turn phase only runs from turn 2 onward). Open-Closed: a new actor kind adds its
    // own spawn stage, this one never changes.
    [UsedImplicitly]
    internal sealed class MayorSpawnSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const int ExecutionPriority = 910;

        private readonly World _world;

        public int Priority => ExecutionPriority;

        public MayorSpawnSystem(World world)
        {
            _world = world;
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            // Idempotent one-shot: an existing allocator means the Mayor was already created
            // (or restored by a future load flow) — never spawn a duplicate on pipeline re-entry.
            if (_world.Has<MayorIdAllocatorComponent>())
                return UniTask.CompletedTask;

            if (!_world.Has<MayorConfigComponent>())
                throw new InvalidOperationException(
                    "MayorSpawnSystem: MayorConfigComponent missing — MayorConfigLoaderSystem must run at ConfigLoadStep first.");

            var config = _world.Get<MayorConfigComponent>();

            _world.Set(new MayorIdAllocatorComponent { Next = 1 });

            // Single actor (id stays 1); allocate-then-advance for symmetry with City and the save/load contract.
            var mayorId = _world.Get<MayorIdAllocatorComponent>().Next;
            _world.Set(new MayorIdAllocatorComponent { Next = mayorId + 1 });

            var mayorIdComponent = new MayorIdComponent { Value = mayorId };
            var mayor = _world.CreateEntity();
            mayor.Set(mayorIdComponent);
            mayor.Set(new ActorTypeComponent { Type = ActorType.Mayor });
            mayor.Set(new MayorAPRestoreComponent { Value = config.StartActionPoints });
            mayor.Set(new MayorAPComponent { Value = config.StartActionPoints });

            ResourceLoadoutSpawner.SpawnLoadout(_world, mayorIdComponent, config.Resources);

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
