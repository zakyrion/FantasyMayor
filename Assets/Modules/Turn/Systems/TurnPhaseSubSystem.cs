using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EcsExtensions;

namespace Modules.Turn.Systems
{
    /// <summary>
    ///     Abstract base for an asynchronous turn phase, collected and run by <see cref="TurnProcessorSystem" />
    ///     through the sub-system contract. Provides <see cref="Priority" /> for ordering within a turn and
    ///     <see cref="IsEnabled" /> for skipping. A phase that needs nested work is itself an orchestrator over
    ///     its own child phases, reusing the same contract — there is no separate recursive base.
    /// </summary>
    public abstract class TurnPhaseSubSystem : IPrioritizedUniTaskSystem, IDisposable
    {
        /// <summary>Determines whether this phase participates in the turn run.</summary>
        public bool IsEnabled { get; set; } = true;

        /// <inheritdoc />
        public Type OrchestratorType => typeof(TurnProcessorSystem);

        /// <summary>Execution order within a turn. Lower values run first.</summary>
        public abstract int Priority { get; }

        /// <inheritdoc />
        public abstract UniTask Update(CancellationToken cancellationToken);

        /// <inheritdoc />
        public virtual void Dispose()
        {
        }
    }
}
