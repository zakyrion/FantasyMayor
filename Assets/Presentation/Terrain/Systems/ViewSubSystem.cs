using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultECSExtensions;

namespace Presentation.Terrain.Systems
{
    /// <summary>
    ///     Abstract base for asynchronous terrain-view subsystems executed sequentially
    ///     by a view orchestrator. Provides <see cref="Priority" /> for execution ordering
    ///     and <see cref="IsEnabled" /> for skipping disabled steps.
    /// </summary>
    internal abstract class ViewSubSystem : IUniTaskSystem<GameState>
    {
        /// <summary>
        ///     Determines whether this subsystem participates in the update loop.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        ///     Execution order within the view pipeline. Lower values run first.
        /// </summary>
        public abstract int Priority { get; }

        /// <inheritdoc />
        public abstract UniTask Update(GameState state, CancellationToken cancellationToken);

        /// <inheritdoc />
        public virtual void Dispose()
        {
        }
    }
}
