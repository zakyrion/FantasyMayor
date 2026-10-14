using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EcsExtensions
{
    /// <summary>
    ///     The one sub-system contract: a part collected by its orchestrator through <see cref="OrchestratorType" />
    ///     and run in ascending <see cref="Priority" /> order. Derives from nothing — a sub-system never carries
    ///     an <c>AppState</c> and is never itself an <see cref="IUniTaskSystem" />.
    /// </summary>
    public interface IPrioritizedUniTaskSystem
    {
        /// <summary>The orchestrator system this sub-system belongs to.</summary>
        Type OrchestratorType { get; }

        /// <summary>Execution order within the orchestrator's run. Lower runs first.</summary>
        int Priority { get; }

        /// <summary>Determines whether this sub-system participates when its orchestrator runs.</summary>
        bool IsEnabled { get; }

        /// <summary>Updates the sub-system once.</summary>
        UniTask Update(CancellationToken cancellationToken);
    }
}
