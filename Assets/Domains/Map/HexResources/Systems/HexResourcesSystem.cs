using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Map.HexResources.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 200). Runs all registered resource generation subsystems
    ///     in priority order. Run by MapCreation's entry pipeline in Priority order, not by an event subscription.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexResourcesSystem : IPipelineStageSystem
    {
        [StateAllowed]
        private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _subSystems;

        /// <inheritdoc />
        public AppState AppState { get; }

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.HexResources;

        /// <param name="appState">The game state this pipeline stage belongs to.</param>
        /// <param name="allSubSystems">Every sub-system on the contract; this host keeps the ones naming it.</param>
        public HexResourcesSystem(AppState appState, IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
        {
            AppState = appState;
            _subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof(HexResourcesSystem), allSubSystems);
        }

        /// <inheritdoc />
        public async UniTask Execute(CancellationToken cancellationToken)
        {
            await OrchestratorSubSystems.RunAsync(_subSystems, cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
