using System;
using EcsExtensions;
using Friflo.Engine.ECS;
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
    ///     Batch dispatch (whole in-progress set reconciled once per ripe pulse, not per-entity), so this
    ///     implements <see cref="IUpdatedSystem" /> directly instead of extending <c>UpdatedSystem</c> — same
    ///     precedent as <c>TurnProcessorSystem</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictCompletionSystem : IUpdatedSystem
    {
        private readonly EntityStore _world;
        private readonly ArchetypeQuery _completePulses;
        private readonly ArchetypeQuery _buildDistrictsInProgress;
        private readonly ComponentIndex<DistrictIdComponent, int> _districtsById;

        public int Priority => SystemPriorities.RuntimeTick.BuildDistrictCompletion;

        public BuildDistrictCompletionSystem(EntityStore world)
        {
            _world = world;
            _completePulses = world.Query<BuildDistrictCompleteEvent>();
            _buildDistrictsInProgress = world.Query<BuildDistrictTurnsComponent, DistrictIdFKComponent>()
                .AllTags(Friflo.Engine.ECS.Tags.Get<BuildDistrictInProgressTag>());
            _districtsById = world.ComponentIndex<DistrictIdComponent, int>();
        }

        // The pulse batch is only the trigger — the work runs over the in-progress set, so however many ripe
        // pulses fired this frame, the set is reconciled once. Snapshot ids first: deleting an entity while
        // enumerating the query that selects it throws StructuralChangeException. Temp allocation happens only
        // on frames where at least one build actually finished; idle pulses return at the zero count.
        public void Update(GameState state)
        {
            var hasRipePulse = false;
            foreach (var pulse in _completePulses.Entities)
                if (EcsEventExtensions.IsRipe(pulse))
                {
                    hasRipePulse = true;
                    break;
                }

            if (!hasRipePulse)
                return;

            var ready = new NativeList<int>(8, Allocator.Temp);
            foreach (var buildingEntity in _buildDistrictsInProgress.Entities)
                if (buildingEntity.GetComponent<BuildDistrictTurnsComponent>().TurnsLeft <= 0)
                    ready.Add(buildingEntity.Id);

            if (ready.Length == 0)
            {
                ready.Dispose();
                return;
            }

            for (var i = 0; i < ready.Length; i++)
            {
                _world.TryGetEntityById(ready[i], out var entity);
                var districtId = entity.GetComponent<DistrictIdFKComponent>().Value;

                if (!_districtsById[districtId].TryGetFirst(out var district))
                    throw new InvalidOperationException(
                        $"BuildDistrictCompletionSystem: no District row for id {districtId} — unify-district-row invariant broken.");

                district.AddComponent(new DistrictBuildStateComponent { Value = DistrictBuildState.Built });

                entity.DeleteEntity();
            }

            ready.Dispose();
            RaiseTableChanged();
        }

        // One pulse per pass (not per row — the multimap filter lets consumers reconcile the whole Built slice):
        // DistrictViewSpawnSystem reconciles the newly-Built District rows against the views it has already
        // spawned.
        private void RaiseTableChanged()
        {
            _world.CreateEvent(new DistrictTableChangedEvent { Change = DistrictTableChange.Built });
        }
    }
}
