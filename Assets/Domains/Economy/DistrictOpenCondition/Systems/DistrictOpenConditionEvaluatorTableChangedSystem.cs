using System.Collections.Generic;
using System.Linq;
using Core;
using EcsExtensions;
using Friflo.Engine.ECS;
using Domains.Economy.District.Events;
using JetBrains.Annotations;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Reactive host (3rd, sibling of the MapGenerationStep bootstrap and TurnPhaseSubSystem hosts): re-runs the
    // condition-evaluator subsystem family on every DistrictTableChangedEvent{Planned/Built/Removed} pulse, so a
    // confirm or cancel re-gates the buildable list within the SAME turn instead of waiting for the next Preview
    // pass. No evaluation logic of its own.
    [UsedImplicitly]
    public sealed class DistrictOpenConditionEvaluatorTableChangedSystem : UpdatedSystem
    {
        [StateAllowed]
        private readonly IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> _subSystems;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictOpenConditionEvaluatorTableChanged;

        public DistrictOpenConditionEvaluatorTableChangedSystem(
            EntityStore world, IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> subSystems)
            : base(world, EventArchetypes.Of<DistrictTableChangedEvent>(world))
        {
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        // The pulse entity is ignored — every subsystem reconciles globally off current world state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            for (var i = 0; i < _subSystems.Count; i++)
                if (_subSystems[i].IsEnabled)
                    _subSystems[i].Evaluate();
        }
    }
}
