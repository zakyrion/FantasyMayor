using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Boot.Core;
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
        private readonly EntityStorages _storages;

        /// <inheritdoc />
        public int Priority => SystemPriorities.RuntimeTick.EventCleanup;

        /// <inheritdoc />
        public AppState AppState { get; }

        public EventCleanupSystem(AppState appState, EntityStorages storages)
        {
            AppState = appState;
            _storages = storages;
            _events = storages.World.Query().AllTags(Tags.Get<EventTag>());
        }

        /// <inheritdoc />
        public void Update(GameState state)
        {
            var ripeIds = new NativeList<int>(8, Allocator.Temp);

            foreach (var entity in _events.Entities)
                if (EcsEventExtensions.IsRipe(entity))
                    ripeIds.Add(entity.Id);

            for (var i = 0; i < ripeIds.Length; i++)
                if (_storages.World.TryGetEntityById(ripeIds[i], out var entity))
                    entity.DeleteEntity();

            ripeIds.Dispose();
        }
    }
}
