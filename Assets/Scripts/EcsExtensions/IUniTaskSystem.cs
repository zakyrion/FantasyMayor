using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Boot.Core;

namespace EcsExtensions
{
    /// <summary>
    ///     Exposes an asynchronous method to update a system.
    /// </summary>
    /// <typeparam name="T">Tags which pipeline stage this system belongs to, for DI collection-grouping only.</typeparam>
    public interface IUniTaskSystem<in T> : IDisposable
    {
        /// <summary>
        ///     Updates the system once.
        /// </summary>
        /// <param name="cancellationToken">The token used to cancel current update.</param>
        /// <returns>A task that represents the asynchronous update.</returns>
        UniTask Update(CancellationToken cancellationToken);
    }

    public interface IUniTaskSystem : IDisposable, IAppStateSystem
    {
        UniTask Execute(CancellationToken cancellationToken);
    }
}
