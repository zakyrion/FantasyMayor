using System.Collections.Generic;
using System.Linq;
using Core;
using EcsExtensions;
using Domains.Economy.District.Events;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Reactive host (3rd, sibling of the map-creation-stage bootstrap and turn-phase hosts): re-runs the
    // condition-evaluator subsystem family on every DistrictTableChangedEvent{Planned/Built/Removed} event, so a
    // confirm or cancel re-gates the buildable list within the SAME turn instead of waiting for the next Preview
    // pass. No evaluation logic of its own.
    [UsedImplicitly]
    public sealed class DistrictOpenConditionEvaluatorTableChangedSystem : IUpdatedSystem
    {
        [StateAllowed]
        private readonly IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> _subSystems;

        private readonly EventReader<DistrictTableChangedEvent> _districtTableChanges;

        public AppState AppState { get; }
        public int Priority => SystemPriorities.RuntimeTick.DistrictOpenConditionEvaluatorTableChanged;

        public DistrictOpenConditionEvaluatorTableChangedSystem(
            AppState appState, EntityStorages storages, EventReader<DistrictTableChangedEvent> districtTableChanges,
            IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> subSystems)
        {
            AppState = appState;
            _ = storages; // kept for the constructor shape shared by every event consumer — this host needs no other table
            _districtTableChanges = districtTableChanges;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        public void Update(GameState state)
        {
            while (_districtTableChanges.TryRead(out _))
                EvaluateOpenConditions();
        }

        // Every subsystem reconciles globally off current world state — the event's payload itself is ignored.
        private void EvaluateOpenConditions()
        {
            for (var i = 0; i < _subSystems.Count; i++)
                if (_subSystems[i].IsEnabled)
                    _subSystems[i].Evaluate();
        }
    }
}
