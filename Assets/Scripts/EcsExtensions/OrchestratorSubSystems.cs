using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EcsExtensions
{
    /// <summary>
    ///     An orchestrator's selection of its sub-systems and their awaited run: the one place that reads
    ///     <see cref="IPrioritizedUniTaskSystem.OrchestratorType" />, replacing every hand-written per-orchestrator
    ///     collection loop and TurnPhaseRunner.
    /// </summary>
    public static class OrchestratorSubSystems
    {
        /// <summary>Keeps the sub-systems that name <paramref name="orchestratorType" />, ascending by <see cref="IPrioritizedUniTaskSystem.Priority" />.</summary>
        /// <param name="orchestratorType">The orchestrator's own type — always <c>typeof(&lt;the orchestrator class&gt;)</c>, never <c>GetType()</c>.</param>
        /// <param name="allSubSystems">Every sub-system the container collected for this contract.</param>
        /// <exception cref="InvalidOperationException">
        ///     No sub-system names <paramref name="orchestratorType" /> — every orchestrator of the contract has
        ///     parts, so an empty selection is a mistyped <see cref="IPrioritizedUniTaskSystem.OrchestratorType" />
        ///     and lost work.
        /// </exception>
        public static IPrioritizedUniTaskSystem[] SelectForOrchestrator(Type orchestratorType, IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
        {
            var parts = allSubSystems.Where(part => part.OrchestratorType == orchestratorType).OrderBy(part => part.Priority).ToArray();
            if (parts.Length == 0)
                throw new InvalidOperationException($"No sub-system names orchestrator '{orchestratorType.Name}'.");

            return parts;
        }

        /// <summary>Awaits every enabled part, in the order given.</summary>
        /// <param name="orchestratorParts">The orchestrator's own sub-systems, in run order.</param>
        /// <param name="cancellationToken">The run's cancellation token.</param>
        /// <exception cref="OperationCanceledException">The run was cancelled.</exception>
        public static async UniTask RunAsync(IReadOnlyList<IPrioritizedUniTaskSystem> orchestratorParts, CancellationToken cancellationToken)
        {
            for (var i = 0; i < orchestratorParts.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var part = orchestratorParts[i];
                if (!part.IsEnabled)
                    continue; // switched off through IsEnabled — every family base carries the switch

                await part.Update(cancellationToken);
            }
        }
    }
}
