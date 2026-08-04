using Friflo.Engine.ECS;
using Unity.Collections;

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
            var entities = _query.Entities;

            // Dispatching Update(state, entity) while _query.Entities is enumerating is a structural change
            // (StructuralChangeException, store-wide — thrown by AddComponent/AddTag/RemoveComponent/RemoveTag
            // anywhere inside the dispatched call, even on an unrelated entity) — snapshot ids first, dispatch
            // via re-fetch.
            var ids = new NativeList<int>(entities.Count, Allocator.Temp);
            try
            {
                foreach (var entity in entities)
                    ids.Add(entity.Id);

                for (var i = 0; i < ids.Length; i++)
                {
                    if (_query.Store.TryGetEntityById(ids[i], out var entity))
                        Update(state, entity);
                }
            }
            finally
            {
                ids.Dispose();
            }
        }

        /// <summary>Called once per matching entity, every frame.</summary>
        protected abstract void Update(GameState state, in Entity entity);
    }
}
