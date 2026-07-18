using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace EcsExtensions
{
    /// <summary>
    ///     Represents a collection of <see cref="IUniTaskSystem{T}" /> to update sequentially.
    ///     Each system fully completes before the next one starts, guaranteeing strict ordering.
    /// </summary>
    /// <typeparam name="T">The type of the object used as state to update the systems.</typeparam>
    public sealed class UniTaskSequentialSystem<T> : IUniTaskSystem<T>
    {
        private readonly IReadOnlyCollection<IUniTaskSystem<T>> _systems;
        private bool _disposed;

        /// <summary>
        ///     Initialises a new instance of the <see cref="UniTaskSequentialSystem{T}" /> class.
        /// </summary>
        /// <param name="systems">The <see cref="IUniTaskSystem{T}" /> instances.</param>
        /// <exception cref="ArgumentNullException"><paramref name="systems" /> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="systems" /> contains a null system.</exception>
        public UniTaskSequentialSystem(IReadOnlyCollection<IUniTaskSystem<T>> systems)
        {
            _systems = systems ?? throw new ArgumentNullException(nameof(systems));
        }

        /// <summary>
        ///     Updates all the systems once sequentially.
        ///     Each system fully completes before the next one starts.
        /// </summary>
        /// <param name="state">The state to use.</param>
        /// <param name="cancellationToken">The token used to cancel the update chain.</param>
        /// <returns>A task that represents the asynchronous sequential update.</returns>
        /// <exception cref="ObjectDisposedException">The system has been disposed.</exception>
        public async UniTask Update(T state, CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(UniTaskSequentialSystem<T>));

            foreach (var system in _systems)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await system.Update(state, cancellationToken);
            }
        }

        /// <summary>
        ///     Disposes all the inner <see cref="IUniTaskSystem{T}" /> instances.
        ///     All systems are disposed even if one or more throw; exceptions are aggregated and rethrown.
        /// </summary>
        /// <exception cref="AggregateException">One or more inner systems threw during disposal.</exception>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            List<Exception> exceptions = null;
            foreach (var system in _systems)
            {
                try
                {
                    system.Dispose();
                }
                catch (Exception ex)
                {
                    (exceptions ??= new List<Exception>()).Add(ex);
                }
            }

            if (exceptions != null)
                throw new AggregateException(exceptions);
        }
    }
}
