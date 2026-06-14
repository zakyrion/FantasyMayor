using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultECSExtensions;
using Modules.Turn.Data;

namespace Modules.Turn.Systems
{
    /// <summary>
    ///     Abstract base for an asynchronous turn phase executed by <c>TurnPhaseRunner</c>. Provides
    ///     <see cref="Priority" /> for ordering within a turn and <see cref="IsEnabled" /> for skipping.
    ///     A phase that needs nested work is itself an orchestrator over its own child phases, reusing the
    ///     same runner — there is no separate recursive base.
    /// </summary>
    public abstract class TurnPhaseSubSystem : IPrioritizedUniTaskSystem<TurnPhaseStep>
    {
        /// <summary>Determines whether this phase participates in the turn run.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>Execution order within a turn. Lower values run first.</summary>
        public abstract int Priority { get; }

        /// <inheritdoc />
        public abstract UniTask Update(TurnPhaseStep state, CancellationToken cancellationToken);

        /// <inheritdoc />
        public virtual void Dispose()
        {
        }
    }
}
