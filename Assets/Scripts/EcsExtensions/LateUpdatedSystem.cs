using Friflo.Engine.ECS;
using Unity.Collections;

namespace EcsExtensions
{
    /// <summary>
    ///     Abstract base for entity-set systems that participate in the per-frame Unity LateUpdate loop.
    ///     A subclass drives off either a single <see cref="Archetype" /> (resolved via its owning holder — the
    ///     default) or, where the trigger genuinely spans more than one archetype (e.g. an <c>AnyComponents</c>
    ///     fan-out), an <see cref="ArchetypeQuery" /> built in its own constructor. Defines relative execution
    ///     order via <see cref="Priority" />. Iterates <c>.Entities</c> — fine at this project's per-set entity
    ///     counts; a system needing chunk-level component access reads it itself instead of going through this base.
    /// </summary>
    public abstract class LateUpdatedSystem : ILateUpdatedSystem
    {
        private readonly Archetype _archetype;
        private readonly EntityStore _archetypeStore;
        private readonly ArchetypeQuery _query;

        /// <inheritdoc />
        public abstract int Priority { get; }

        /// <param name="store">The store to re-fetch entities from after the id snapshot.</param>
        /// <param name="archetype">The single archetype this system iterates each frame.</param>
        protected LateUpdatedSystem(EntityStore store, Archetype archetype)
        {
            _archetypeStore = store;
            _archetype = archetype;
        }

        /// <param name="query">A query spanning more than one archetype this system iterates each frame.</param>
        protected LateUpdatedSystem(ArchetypeQuery query)
        {
            _query = query;
        }

        /// <inheritdoc />
        public void Update(GameState state)
        {
            var entities = _archetype != null ? _archetype.Entities : _query.Entities;
            var store = _archetype != null ? _archetypeStore : _query.Store;

            // Dispatching Update(state, entity) while entities is enumerating is a structural change
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
                    if (store.TryGetEntityById(ids[i], out var entity))
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
