using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Unity.Collections;

namespace EcsExtensions
{
    /// <summary>
    ///     Disposes one-frame event entities once <see cref="EcsEventExtensions.IsRipe" /> — after every
    ///     consumer had its full frame to react (law: ECS_CONVENTIONS.md → Event Lifecycle).
    ///     Snapshots ripe entity ids first: deleting mid-enumeration throws StructuralChangeException, and
    ///     an <see cref="Entity" /> itself is not unmanaged (it carries a store reference), so the scratch
    ///     buffer holds ids, not entities.
    /// </summary>
    [UsedImplicitly]
    public sealed class EventCleanupSystem : IUpdatedSystem
    {
        private readonly ArchetypeQuery _events;
        private readonly EntityStore _store;

        /// <inheritdoc />
        public int Priority => SystemPriorities.RuntimeTick.EventCleanup;

        public EventCleanupSystem(EntityStore store)
        {
            _store = store;
            _events = store.Query().AllTags(Tags.Get<EventTag>());
        }

        /// <inheritdoc />
        public void Update(GameState state)
        {
            var ripeIds = new NativeList<int>(8, Allocator.Temp);

            foreach (var entity in _events.Entities)
                if (EcsEventExtensions.IsRipe(entity))
                    ripeIds.Add(entity.Id);

            for (var i = 0; i < ripeIds.Length; i++)
                if (_store.TryGetEntityById(ripeIds[i], out var entity))
                    entity.DeleteEntity();

            ripeIds.Dispose();
        }
    }
}
