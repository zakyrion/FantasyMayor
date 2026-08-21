using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EcsExtensions
{
    /// <summary>
    ///     Exposes an asynchronous method to update a system.
    /// </summary>
    /// <typeparam name="T">The type of the object used as state to update the system.</typeparam>
    public interface IUniTaskSystem<in T> : IDisposable
    {
        /// <summary>
        ///     Updates the system once.
        ///     Does nothing if <see cref="IsEnabled" /> is false.
        /// </summary>
        /// <param name="state">The state to use.</param>
        /// <param name="cancellationToken">The token used to cancel current update.</param>
        /// <returns>A task that represents the asynchronous update.</returns>
        UniTask Update(T state, CancellationToken cancellationToken);
    }
}
