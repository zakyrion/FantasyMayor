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
    ///     skips disabled phases, and awaits each in turn. Threading is the caller's concern: today the only
    ///     caller, <see cref="TurnProcessorSystem" />, runs the phase set inline on the main thread, because
    ///     phases do store I/O and store I/O is main-thread only (ECS_CONVENTIONS → Threading And Native
    ///     Memory, Law 1). This runner adds no hop of its own.
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
