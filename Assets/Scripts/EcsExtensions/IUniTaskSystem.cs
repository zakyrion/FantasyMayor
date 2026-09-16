using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Boot.Core;

namespace EcsExtensions
{
    /// <summary>
    ///     The non-generic async system contract: a first-order one-shot system, run to completion once by the
    ///     game state it is flagged for.
    /// </summary>
    public interface IUniTaskSystem : IDisposable, IAppStateSystem
    {
        /// <summary>
        ///     Runs the system once.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel the run.</param>
        /// <returns>A task that represents the asynchronous run.</returns>
        UniTask Execute(CancellationToken cancellationToken);
    }
}
