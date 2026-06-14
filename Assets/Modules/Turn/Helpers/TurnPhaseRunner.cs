using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Turn.Data;
using Modules.Turn.Systems;

namespace Modules.Turn.Helpers
{
    /// <summary>
    ///     Stateless sequential runner for turn phases: orders by <see cref="TurnPhaseSubSystem.Priority" />,
    ///     skips disabled phases, and awaits each in turn. Threading is the caller's concern (the launcher owns
    ///     the thread-pool hop), so a nested phase-orchestrator can reuse this on the same pool thread.
    /// </summary>
    public sealed class TurnPhaseRunner
    {
        public async UniTask RunAsync(
            IReadOnlyList<TurnPhaseSubSystem> phases,
            TurnPhaseStep step,
            CancellationToken cancellationToken)
        {
            foreach (var phase in phases.OrderBy(candidate => candidate.Priority))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!phase.IsEnabled)
                    continue;

                await phase.Update(step, cancellationToken);
            }
        }
    }
}
