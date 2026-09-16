using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Economy.DistrictOpenCondition.Systems
{
    // Map-creation stage: runs the condition-evaluator subsystem family once, right after
    // DistrictOpenConditionSpawnSystem, so DistrictOpenStateComponent is already correct before the player's
    // very first Mayor Phase. The turn pipeline never runs a bootstrap pass before turn 1 (no startup
    // Preview), so this world-init hook is what covers it. Every subsequent turn is covered by the sibling
    // TurnPhaseSubSystem host (DistrictOpenConditionEvaluatorSystem) — both share the same DI-collected
    // subsystem family, no evaluation logic is duplicated here. No domain logic of its own. The evaluator family
    // itself is untouched by the sub-system contract (owner rewrites it later) — only this host's own shape,
    // shared by every first-order system, changes.
    [UsedImplicitly]
    internal sealed class DistrictOpenConditionEvaluatorBootstrapSystem : IPipelineStageSystem
    {
        [StateAllowed]
        private readonly IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> _subSystems;

        public AppState AppState { get; }

        public int Priority => SystemPriorities.WorldInit.DistrictOpenConditionEvaluatorBootstrap;

        public DistrictOpenConditionEvaluatorBootstrapSystem(
            AppState appState, IReadOnlyList<DistrictOpenConditionEvaluatorSubSystem> subSystems)
        {
            AppState = appState;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        public UniTask Execute(CancellationToken cancellationToken)
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
