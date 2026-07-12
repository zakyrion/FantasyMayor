using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;

namespace Domains.Map.HexResources.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 200). Runs all registered resource generation subsystems
    ///     in priority order. Driven by the Boot world-init orchestrator, not by an event subscription.
    /// </summary>
    [UsedImplicitly]
    internal sealed class HexResourcesSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private readonly IReadOnlyList<HexResourcesSubSystem> _resourceSubSystems;

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.HexResources;

        public HexResourcesSystem(IReadOnlyList<HexResourcesSubSystem> resourceSubSystems)
        {
            _resourceSubSystems = resourceSubSystems
                .OrderBy(system => system.Priority)
                .ToArray();
        }

        /// <inheritdoc />
        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            // Resource generation is one-shot; deltaTime is irrelevant, so a default GameState is passed through.
            var gameState = default(GameState);

            foreach (var resourceSubSystem in _resourceSubSystems)
            {
                if (!resourceSubSystem.IsEnabled)
                    continue;

                resourceSubSystem.Update(gameState);
            }

            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
