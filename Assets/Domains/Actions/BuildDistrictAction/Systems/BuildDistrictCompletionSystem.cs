using System;
using Domains.Actions.Archetypes;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Unity.Collections;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Event-gated reconcile: on <c>BuildDistrictCompleteEvent</c> (raised on the main thread by
    ///     <c>BuildDistrictTurnTickSystem</c>, once per turn, the first time any countdown reaches zero — see
    ///     :lossy-producer) flips finished builds' District row to Built — every in-progress verb row
    ///     (<see cref="BuildDistrictInProgressTag" />) whose countdown reached zero resolves its
    ///     <c>DistrictIdFKComponent</c> to the District row (allocated at CONFIRM by
    ///     <c>BuildDistrictActionSystem</c> — the fact already exists, this system does NOT create it), Sets it
    ///     <c>DistrictBuildState.Built</c>, then disposes the verb row; one <c>DistrictTableChangedEvent</c>
    ///     {Built} covers the pass for the world-view spawner. This system reconciles the whole in-progress set
    ///     off state rather than trusting a one-to-one delivery — an event evicted before this system reads it
    ///     (the queue only holds so many) leaves the affected build stuck at zero turns left, an accepted risk
    ///     (CASCADE.md contra c-2, decision :completion-eviction-risk). See <c>Flows/FLOW_DISTRICT_BUILD.md</c>.
    ///     Batch dispatch (whole in-progress set reconciled once per non-empty batch, not per-event), so this
    ///     implements <see cref="IUpdatedSystem" /> directly instead of extending <c>UpdatedSystem</c> — same
    ///     precedent as <c>TurnProcessorSystem</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictCompletionSystem : IUpdatedSystem
    {
        private readonly EntityStorages _storages;
        private readonly EventReader<BuildDistrictCompleteEvent> _buildCompletions;
        private readonly Archetype _buildDistrictsInProgress;
        private readonly ComponentIndex<DistrictIdComponent, int> _districtsById;

        public AppState AppState { get; }

        public int Priority => SystemPriorities.RuntimeTick.BuildDistrictCompletion;

        public BuildDistrictCompletionSystem(AppState appState, EntityStorages storages, EventReader<BuildDistrictCompleteEvent> buildCompletions)
        {
            AppState = appState;
            _storages = storages;
            _buildCompletions = buildCompletions;
            _buildDistrictsInProgress = ActionsArchetypes.BuildDistrictInProgress(storages.World);
            _districtsById = storages.World.ComponentIndex<DistrictIdComponent, int>();
        }

        // The batch is only the trigger — the work runs over the in-progress set, so however many events fired
        // this frame, the set is reconciled once. Snapshot ids first: deleting an entity while enumerating the
        // query that selects it throws StructuralChangeException. Temp allocation happens only on frames where
        // at least one build actually finished; an empty batch returns before it.
        public void Update(GameState state)
        {
            if (!_buildCompletions.DrainBatch())
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
                _storages.World.TryGetEntityById(ready[i], out var entity);
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

        // One event per pass (not per row — the multimap filter lets consumers reconcile the whole Built slice):
        // DistrictViewSpawnSystem reconciles the newly-Built District rows against the views it has already
        // spawned.
        private void RaiseTableChanged()
        {
            _storages.Events.Raise(new DistrictTableChangedEvent { Change = DistrictTableChange.Built });
        }
    }
}
