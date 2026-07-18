using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     Abstract base for entity-set systems that participate in the per-frame Unity Update loop.
    ///     Subclasses build their <see cref="ArchetypeQuery" /> in their own constructor (e.g. <c>store.Query&lt;T&gt;()</c>)
    ///     and define their relative execution order via <see cref="Priority" />. Iterates <c>query.Entities</c> —
    ///     fine at this project's per-set entity counts; a system needing chunk-level component access reads it
    ///     itself instead of going through this base.
    /// </summary>
    public abstract class UpdatedSystem : IUpdatedSystem
    {
        private readonly ArchetypeQuery _query;

        /// <inheritdoc />
        public abstract int Priority { get; }

        /// <param name="query">The query this system iterates each frame.</param>
        protected UpdatedSystem(ArchetypeQuery query)
        {
            _query = query;
        }

        /// <inheritdoc />
        public void Update(GameState state)
        {
            foreach (var entity in _query.Entities)
                Update(state, entity);
        }

        /// <summary>Called once per matching entity, every frame.</summary>
        protected abstract void Update(GameState state, in Entity entity);
    }
}
