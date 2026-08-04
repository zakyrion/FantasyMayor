using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using Domains.Actors.Archetypes;
using Domains.Actors.Components;
using Domains.Kernel.Data;
using Domains.Actors.Mayor.Components;
using Domains.Actors.Mayor.Tags;
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
        private readonly EntityStore _world;
        private readonly Archetype _mayorArchetype;

        public int Priority => SystemPriorities.WorldInit.MayorSpawn;

        public MayorSpawnSystem(EntityStore world)
        {
            _world = world;
            _mayorArchetype = ActorsArchetypes.Mayor(world);
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            // Idempotent one-shot: an existing allocator means the Mayor was already created
            // (or restored by a future load flow) — never spawn a duplicate on pipeline re-entry.
            if (_world.HasWorldComponent<MayorIdAllocatorComponent>())
                return UniTask.CompletedTask;

            if (!_world.HasWorldComponent<MayorConfigComponent>())
                throw new InvalidOperationException(
                    "MayorSpawnSystem: MayorConfigComponent missing — MayorConfigLoaderSystem must run at ConfigLoadStep first.");

            var config = _world.GetWorldComponent<MayorConfigComponent>();

            _world.SetWorldComponent(new MayorIdAllocatorComponent { Next = 1 });

            // Single actor (id stays 1); allocate-then-advance for symmetry with City and the save/load contract.
            var mayorId = _world.GetWorldComponent<MayorIdAllocatorComponent>().Next;
            _world.SetWorldComponent(new MayorIdAllocatorComponent { Next = mayorId + 1 });

            var mayorIdComponent = new MayorIdComponent { Value = mayorId };
            var mayor = _mayorArchetype.CreateEntity();
            mayor.AddComponent(mayorIdComponent);
            mayor.AddComponent(new ActorTypeComponent { Type = ActorType.Mayor });
            mayor.AddComponent(new MayorAPRestoreComponent { Value = config.StartActionPoints });
            mayor.AddComponent(new MayorAPComponent { Value = config.StartActionPoints });

            ResourceLoadoutSpawner.SpawnLoadout<MayorIdFKComponent, MayorResourceTag>(
                _world, new MayorIdFKComponent { Value = mayorId }, config.Resources);

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
