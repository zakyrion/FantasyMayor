using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Modules.Turn.Data;
using Modules.Turn.Systems;
using EcsExtensions;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Turn phase (tail / Preview band): every turn, re-runs the condition-evaluator subsystem family so
    // DistrictOpenStateComponent stays correct for the next Mayor Phase. Turn 1 is covered separately by the
    // sibling MapGenerationStep host (DistrictOpenConditionEvaluatorBootstrapSystem) — both share the same
    // DI-collected subsystem family, no evaluation logic is duplicated. No domain logic of its own.
    [UsedImplicitly]
    internal sealed class DistrictOpenConditionEvaluatorSystem : TurnPhaseSubSystem
    {
        [StateAllowed]
        private readonly IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> _subSystems;

        public override int Priority => SystemPriorities.TurnPhase.DistrictOpenConditionEvaluator;

        public DistrictOpenConditionEvaluatorSystem(IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> subSystems)
        {
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        public override async UniTask Update(TurnPhaseStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            // No main-thread hop: this family only Sets EXISTING components (state columns), a VALUE write
            // legal off-thread under ECS_CONVENTIONS Law 1 — hopping here would cost a frame for nothing.
            for (var i = 0; i < _subSystems.Count; i++)
            {
                if (_subSystems[i].IsEnabled)
                    _subSystems[i].Evaluate();
            }
        }
    }
}
