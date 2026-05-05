using DefaultEcs;
using DefaultEcs.System;

namespace DefaultECSExtensions
{
    /// <summary>
    ///     Abstract base for entity-set systems that participate in the per-frame Unity Update loop.
    ///     Subclasses declare which entities to process via the <see cref="EntitySet" /> constructor argument
    ///     and define their relative execution order via <see cref="Priority" />.
    /// </summary>
    public abstract class UpdatedSystem : AEntitySetSystem<GameState>, IUpdatedSystem
    {
        /// <inheritdoc />
        public abstract int Priority { get; }

        /// <param name="entitySet">The filtered set of entities this system operates on each frame.</param>
        protected UpdatedSystem(EntitySet entitySet) : base(entitySet)
        {
        }
    }
}
