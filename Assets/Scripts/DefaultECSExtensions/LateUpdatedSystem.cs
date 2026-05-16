using DefaultEcs;
using DefaultEcs.System;

namespace DefaultECSExtensions
{
    /// <summary>
    ///     Abstract base for entity-set systems that participate in the per-frame Unity LateUpdate loop.
    ///     Subclasses declare which entities to process via the <see cref="EntitySet" /> constructor argument
    ///     and define their relative execution order via <see cref="Priority" />.
    /// </summary>
    public abstract class LateUpdatedSystem : AEntitySetSystem<GameState>, ILateUpdatedSystem
    {
        /// <inheritdoc />
        public abstract int Priority { get; }

        /// <param name="entitySet">The filtered set of entities this system operates on each frame.</param>
        protected LateUpdatedSystem(EntitySet entitySet) : base(entitySet)
        {
        }
    }
}
