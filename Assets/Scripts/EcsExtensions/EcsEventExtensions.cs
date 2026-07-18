using Friflo.Engine.ECS;
using UnityEngine;

namespace EcsExtensions
{
    /// <summary>
    ///     Stateless helpers for the one-frame Event Lifecycle: every event is delivered to ALL consumers
    ///     exactly once, one full frame after creation, independent of system priority — see
    ///     PLAN_FRIFLO_MIGRATION.md → Event Lifecycle spec. <see cref="EventCleanupSystem" /> disposes an
    ///     event once <see cref="IsRipe" /> is true for it.
    /// </summary>
    public static class EcsEventExtensions
    {
        /// <summary>Creates a one-frame event entity, stamped with the current frame.</summary>
        public static Entity CreateEvent<T>(this EntityStore store, in T payload) where T : struct, IComponent
        {
            var e = store.CreateEntity(new EventFrameComponent { Frame = Time.frameCount }, Tags.Get<EventTag>());
            e.AddComponent(payload);
            return e;
        }

        /// <summary>True exactly one full frame after the event's creation — the frame it must be consumed.</summary>
        public static bool IsRipe(in Entity eventEntity) =>
            eventEntity.GetComponent<EventFrameComponent>().Frame < Time.frameCount;
    }
}
