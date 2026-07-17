using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Pipeline stage (MapGenerationStep, one-shot): runs the condition-evaluator subsystem family once, right
    // after DistrictOpenConditionSpawnSystem, so DistrictOpenStateComponent is already correct before the player's
    // very first Mayor Phase. The turn pipeline never runs a bootstrap pass before turn 1 (no startup
    // Preview), so this world-init hook is what covers it. Every subsequent turn is covered by the sibling
    // TurnPhaseSubSystem host (DistrictOpenConditionEvaluatorSystem) — both share the same DI-collected
    // subsystem family, no evaluation logic is duplicated here. No domain logic of its own.
    [UsedImplicitly]
    internal sealed class DistrictOpenConditionEvaluatorBootstrapSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        [StateAllowed]
        private readonly IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> _subSystems;

        public int Priority => SystemPriorities.WorldInit.DistrictOpenConditionEvaluatorBootstrap;

        public DistrictOpenConditionEvaluatorBootstrapSystem(
            IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> subSystems)
        {
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.CompletedTask;

            for (var i = 0; i < _subSystems.Count; i++)
            {
                if (_subSystems[i].IsEnabled)
                    _subSystems[i].Evaluate();
            }

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
