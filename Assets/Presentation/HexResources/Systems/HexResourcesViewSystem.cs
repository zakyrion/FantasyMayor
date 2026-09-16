using System.Collections.Generic;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Presentation.HexResources.Systems
{
    /// <summary>
    ///     Map-creation pipeline stage (priority 400). Runs all registered resource view subsystems
    ///     in priority order.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexResourcesViewSystem : IPipelineStageSystem
    {
        [StateAllowed("DI-collected sub-systems, selected once by the constructor and read only by the awaited run.")]
        private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _subSystems;

        /// <inheritdoc />
        public AppState AppState { get; }

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.HexResourcesView;

        public HexResourcesViewSystem(AppState appState, IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
        {
            AppState = appState;
            _subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof(HexResourcesViewSystem), allSubSystems);
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
