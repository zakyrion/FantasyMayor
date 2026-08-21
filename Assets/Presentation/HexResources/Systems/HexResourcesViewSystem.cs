using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Presentation.HexResources.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 400). Runs all registered resource view subsystems
    ///     in priority order. Driven by the Boot world-init orchestrator, not by an event subscription.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexResourcesViewSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private readonly IReadOnlyList<HexResourcesViewSubSystem> _viewSubSystems;

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.HexResourcesView;

        public HexResourcesViewSystem(IReadOnlyList<HexResourcesViewSubSystem> viewSubSystems)
        {
            _viewSubSystems = viewSubSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        /// <inheritdoc />
        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            // View building is one-shot; deltaTime is irrelevant, so a default GameState is passed through.
            var gameState = default(GameState);

            foreach (var viewSubSystem in _viewSubSystems)
            {
                if (!viewSubSystem.IsEnabled)
                    continue;

                viewSubSystem.Update(gameState);
            }

            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
