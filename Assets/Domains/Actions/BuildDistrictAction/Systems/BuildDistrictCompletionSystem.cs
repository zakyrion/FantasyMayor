using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Components;
using Domains.Economy.District.Tags;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Unity.Collections;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Event-gated reconcile: on the <c>BuildDistrictCompleteEvent</c> pulse (raised on the main thread by
    ///     <c>BuildDistrictTurnTickSystem</c>) it materialises finished builds into Economy District facts — every
    ///     in-progress verb entity (<see cref="BuildDistrictInProgressTag" />) whose countdown reached zero becomes a
    ///     District fact (<c>DistrictTag</c> + allocated <c>DistrictIdComponent</c> PK + its <c>HexIdFKComponent</c> +
    ///     <c>DistrictTypeComponent</c>); the verb entity is disposed and one <c>DistrictBuiltEvent</c> covers the
    ///     pass for the world-view spawner. The pulse is a DOORBELL, not a payload: this system reconciles the whole
    ///     in-progress set off state, and the producer re-raises the pulse EVERY turn while any countdown sits at
    ///     zero (level-triggered) — a pulse lost to the EventCleanup frame window costs one turn of latency, never
    ///     correctness. Both halves of that discipline are the contract; weakening either breaks the mechanic
    ///     silently. See <c>Flows/FLOW_DISTRICT_BUILD.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictCompletionSystem : UpdatedSystem
    {
        private readonly World _world;
        private readonly EntitySet _buildDistrictsInProgress;

        public override int Priority => SystemPriorities.RuntimeTick.BuildDistrictCompletion;

        public BuildDistrictCompletionSystem(World world)
            : base(world.GetEntities().With<BuildDistrictCompleteEvent>().AsSet())
        {
            _world = world;
            _buildDistrictsInProgress = world.GetEntities()
                .With<BuildDistrictInProgressTag>()
                .With<BuildDistrictTurnsComponent>()
                .With<HexIdFKComponent>()
                .With<DistrictTypeFKComponent>()
                .AsSet();

            // Seed the district-id counter once; ids start at 1 (0 = unset).
            if (!world.Has<DistrictIdAllocatorComponent>())
                world.Set(new DistrictIdAllocatorComponent { Next = 1 });
        }

        // Batch override: the pulse batch is only the trigger — the work runs over the in-progress set, so however
        // many pulses fired this frame, the set is reconciled once. Snapshot-before-dispose: disposing during the
        // set's own span iteration only works by leaning on DefaultEcs swap-remove internals (frozen span length +
        // uncleared tail slot) — copy the ready entities out first instead. Temp allocation happens only on frames
        // where at least one build actually finished; idle pulses return at the zero count.
        protected override void Update(GameState state, ReadOnlySpan<Entity> entities)
        {
            var inProgress = _buildDistrictsInProgress.GetEntities();

            var readyCount = 0;
            foreach (var buildingEntity in inProgress)
                if (buildingEntity.Get<BuildDistrictTurnsComponent>().TurnsLeft <= 0)
                    readyCount++;

            if (readyCount == 0)
                return;

            var ready = new NativeArray<Entity>(readyCount, Allocator.Temp);
            var next = 0;
            foreach (var buildingEntity in inProgress)
                if (buildingEntity.Get<BuildDistrictTurnsComponent>().TurnsLeft <= 0)
                    ready[next++] = buildingEntity;

            for (var i = 0; i < ready.Length; i++)
            {
                var entity = ready[i];
                var coords = entity.Get<HexIdFKComponent>().Coords;
                var type = entity.Get<DistrictTypeFKComponent>().Value;

                var fact = _world.CreateEntity();
                fact.Set(new DistrictTag());
                fact.Set(new DistrictIdComponent { Value = AllocateDistrictId() });
                fact.Set(new HexIdFKComponent { Coords = coords });
                fact.Set(new DistrictTypeComponent { Value = type });

                entity.Dispose();
            }

            ready.Dispose();
            RaiseDistrictBuilt();
        }

        // Hands out the next unique district id and advances the shared counter (write via Set).
        private int AllocateDistrictId()
        {
            var id = _world.Get<DistrictIdAllocatorComponent>().Next;
            _world.Set(new DistrictIdAllocatorComponent { Next = id + 1 });
            return id;
        }

        // One payload-less pulse covers however many facts were just written: DistrictViewSpawnSystem reconciles
        // the new District facts against the views it has already spawned.
        private void RaiseDistrictBuilt()
        {
            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuiltEvent());
            entity.Set(new EventTag());
        }
    }
}
