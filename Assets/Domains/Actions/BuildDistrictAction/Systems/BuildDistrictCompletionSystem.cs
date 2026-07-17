using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Economy.District.Tags;
using JetBrains.Annotations;
using Unity.Collections;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Event-gated reconcile: on the <c>BuildDistrictCompleteEvent</c> pulse (raised on the main thread by
    ///     <c>BuildDistrictTurnTickSystem</c>) flips finished builds' District row to Built — every in-progress
    ///     verb row (<see cref="BuildDistrictInProgressTag" />) whose countdown reached zero resolves its
    ///     <c>DistrictIdFKComponent</c> to the District row (allocated at CONFIRM by
    ///     <c>BuildDistrictActionSystem</c> — the fact already exists, this system does NOT create it), Sets it
    ///     <c>DistrictBuildState.Built</c>, then disposes the verb row; one <c>DistrictTableChangedEvent</c>
    ///     {Built} covers the pass for the world-view spawner. The pulse is a DOORBELL, not a payload: this system reconciles the whole
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
        private readonly EntityMap<DistrictIdComponent> _districtsById;

        public override int Priority => SystemPriorities.RuntimeTick.BuildDistrictCompletion;

        public BuildDistrictCompletionSystem(World world)
            : base(world.GetEntities().With<BuildDistrictCompleteEvent>().AsSet())
        {
            _world = world;
            _buildDistrictsInProgress = world.GetEntities()
                .With<BuildDistrictInProgressTag>()
                .With<BuildDistrictTurnsComponent>()
                .With<DistrictIdFKComponent>()
                .AsSet();
            _districtsById = world.GetEntities()
                .With<DistrictTag>()
                .With<DistrictIdComponent>()
                .AsMap<DistrictIdComponent>();
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
                var districtId = entity.Get<DistrictIdFKComponent>().Value;

                if (!_districtsById.TryGetEntity(new DistrictIdComponent { Value = districtId }, out var district))
                    throw new InvalidOperationException(
                        $"BuildDistrictCompletionSystem: no District row for id {districtId} — unify-district-row invariant broken.");

                district.Set(new DistrictBuildStateComponent { Value = DistrictBuildState.Built });

                entity.Dispose();
            }

            ready.Dispose();
            RaiseTableChanged();
        }

        // One pulse per pass (not per row — the multimap filter lets consumers reconcile the whole Built slice):
        // DistrictViewSpawnSystem reconciles the newly-Built District rows against the views it has already
        // spawned.
        private void RaiseTableChanged()
        {
            var entity = _world.CreateEntity();
            entity.Set(new DistrictTableChangedEvent { Change = DistrictTableChange.Built });
            entity.Set(new EventTag());
        }

        public override void Dispose()
        {
            _buildDistrictsInProgress.Dispose();
            _districtsById.Dispose();
            base.Dispose();
        }
    }
}
