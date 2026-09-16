using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     The event log: entities of every event type live in their own <see cref="EntityStore" />, one ring
    ///     per type, capped at that type's capacity (<see cref="EventCapacityAttribute" />). Raising an event
    ///     past capacity evicts the oldest of its type — nothing else deletes an event. Each reader's cursor is
    ///     a row this log owns; <see cref="EventReader{TEvent}" /> holds only its id.
    /// </summary>
    public sealed class EventLog
    {
        private readonly EntityStore _store;
        private readonly EntitySchema _schema;
        private readonly EventRing[] _ringsByEventType;
        private readonly List<long> _cursors;
        private long _lastSequence;

        internal EventLog()
        {
            _store = new EntityStore();
            _schema = EntityStore.GetEntitySchema();
            _ringsByEventType = new EventRing[_schema.Components.Length];
            _cursors = new List<long>();
        }

        /// <summary>
        ///     Raises an event: it becomes the newest in its type's ring under the next global sequence number.
        ///     A ring at capacity evicts its oldest event first — overflow never throws, the event that missed
        ///     it is simply gone for whoever had not read it yet.
        /// </summary>
        public void Raise<TEvent>(in TEvent evt) where TEvent : struct, IEventTag
        {
            var ring = RingOf<TEvent>();
            EvictOldestWhenFull(ring);
            var slot = BirthEventEntity(ring, evt);
            ring.Append(slot);
        }

        private void EvictOldestWhenFull(EventRing ring)
        {
            if (ring.IsFull)
                EvictOldest(ring);
        }

        // second caller: ClearAllEvents
        private void EvictOldest(EventRing ring)
        {
            _store.GetEntityById(ring.Oldest.EntityId).DeleteEntity();
            ring.DropOldest();
        }

        private EventRingSlot BirthEventEntity<TEvent>(EventRing ring, in TEvent evt) where TEvent : struct, IEventTag
        {
            var entity = ring.Archetype.CreateEntity();
            entity.AddComponent(evt);
            _lastSequence++;
            return new EventRingSlot(entity.Id, _lastSequence);
        }

        /// <summary>Opens a new cursor at "no offset yet" — its first read returns the oldest present event.</summary>
        internal int OpenCursor<TEvent>() where TEvent : struct, IEventTag
        {
            RingOf<TEvent>(); // touches the type so its ring and capacity exist before the owner's first tick
            _cursors.Add(0);
            return _cursors.Count - 1;
        }

        /// <summary>Hands the cursor's next unread event of its type. False when none is left.</summary>
        internal bool TryRead<TEvent>(int cursorId, out TEvent evt) where TEvent : struct, IEventTag
        {
            var ring = RingOf<TEvent>();
            var offset = ClampedCursor(ring, cursorId);

            if (offset == ring.AppendedCount)
            {
                evt = default;
                return false;
            }

            evt = DeliverEventAt<TEvent>(ring, cursorId, offset);
            return true;
        }

        private long ClampedCursor(EventRing ring, int cursorId)
        {
            var offset = _cursors[cursorId];

            if (offset < ring.OldestOffset)
            {
                offset = ring.OldestOffset;
                _cursors[cursorId] = offset;
            }

            return offset;
        }

        private TEvent DeliverEventAt<TEvent>(EventRing ring, int cursorId, long offset) where TEvent : struct, IEventTag
        {
            var slot = ring.SlotAt(offset);
            var evt = _store.GetEntityById(slot.EntityId).GetComponent<TEvent>();
            _cursors[cursorId] = offset + 1;
            return evt;
        }

        /// <summary>Deletes every event of every type. Cursors are untouched — each catches up to empty on its next read.</summary>
        public void ClearAllEvents()
        {
            foreach (var ring in _ringsByEventType)
            {
                if (ring == null)
                    continue;

                while (!ring.IsEmpty)
                    EvictOldest(ring);
            }
        }

        // Raise, OpenCursor and TryRead are equally-ranked entry points of this class-API, each touching a
        // type's ring on first use — so the shared helpers they call live here, after all three.
        private EventRing RingOf<TEvent>() where TEvent : struct, IEventTag
        {
            var eventType = _schema.GetComponentType<TEvent>();

            if (eventType == null)
                throw new InvalidOperationException(
                    $"Friflo's schema does not see {typeof(TEvent).Name} as a component through IEventTag.");

            var ring = _ringsByEventType[eventType.StructIndex];

            if (ring != null)
                return ring;

            ring = new EventRing(_store.GetArchetype(ComponentTypes.Get<TEvent>(), default), CapacityOf<TEvent>());
            _ringsByEventType[eventType.StructIndex] = ring;
            return ring;
        }

        private static int CapacityOf<TEvent>() where TEvent : struct, IEventTag =>
            typeof(TEvent).GetCustomAttribute<EventCapacityAttribute>()?.Capacity ?? EventCapacityAttribute.DefaultCapacity;
    }
}
