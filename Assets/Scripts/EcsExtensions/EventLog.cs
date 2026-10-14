using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Engine.ECS;
using Core;

namespace EcsExtensions
{
    /// <summary>
    ///     The event log: entities of every event type live in their own <see cref="EntityStore" />, one ring
    ///     per type, capped at that type's capacity (<see cref="EventCapacityAttribute" />). Raising an event
    ///     past capacity evicts the oldest of its type — nothing else deletes an event. Each reader's cursor is
    ///     a row this log owns; <see cref="EventReader{TEvent}" /> holds only its id and hands the row back when
    ///     it is disposed, so the next reader opened reuses it.
    /// </summary>
    public sealed class EventLog
    {
        private const long ClosedCursor = -1;   // a released row, waiting in _freeCursors for the next reader

        private readonly List<long> _cursors;
        private readonly Stack<int> _freeCursors;
        private readonly EventRing[] _ringsByEventType;
        private readonly EntitySchema _schema;
        private readonly EntityStore _store;

        internal EventLog()
        {
            _store = new EntityStore();
            _schema = EntityStore.GetEntitySchema();
            _ringsByEventType = new EventRing[_schema.Components.Length];
            _cursors = new List<long>();
            _freeCursors = new Stack<int>();
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

        /// <summary>
        ///     Raises an event: it becomes the newest in its type's ring, and its type counts one more raise.
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

        /// <summary>
        ///     Opens a cursor at "no offset yet" — its first read returns the oldest present event. A row a
        ///     disposed reader released is reused before a new one is added.
        /// </summary>
        internal int OpenCursor<TEvent>() where TEvent : struct, IEventTag
        {
            RingOf<TEvent>(); // touches the type so its ring and capacity exist before the owner's first tick

            if (_freeCursors.TryPop(out var released))
            {
                _cursors[released] = 0;
                return released;
            }

            _cursors.Add(0);
            return _cursors.Count - 1;
        }

        /// <summary>Releases a cursor for the next reader; a cursor released twice throws.</summary>
        /// <exception cref="InvalidOperationException">The cursor was already released.</exception>
        internal void CloseCursor(int cursorId)
        {
            if (_cursors[cursorId] == ClosedCursor)
                throw new InvalidOperationException($"Event cursor {cursorId} was already released.");

            _cursors[cursorId] = ClosedCursor;
            _freeCursors.Push(cursorId);
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

        private static int CapacityOf<TEvent>() where TEvent : struct, IEventTag
        {
            return typeof(TEvent).GetCustomAttribute<EventCapacityAttribute>()?.Capacity ?? EventCapacityAttribute.DefaultCapacity;
        }

        private EventRingSlot BirthEventEntity<TEvent>(EventRing ring, in TEvent evt) where TEvent : struct, IEventTag
        {
            var entity = ring.Archetype.CreateEntity();
            entity.AddComponent(evt);
            return new EventRingSlot(entity.Id);
        }

        private long ClampedCursor(EventRing ring, int cursorId)
        {
            var offset = _cursors[cursorId];

            if (offset == ClosedCursor)
                throw new InvalidOperationException($"Event cursor {cursorId} was released — its reader was disposed.");

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

        // second caller: ClearAllEvents
        private void EvictOldest(EventRing ring)
        {
            _store.GetEntityById(ring.Oldest.EntityId).DeleteEntity();
            ring.DropOldest();
        }

        private void EvictOldestWhenFull(EventRing ring)
        {
            if (ring.IsFull)
                EvictOldest(ring);
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

            ring = new EventRing(_store.GetArchetype(ComponentTypes.Get<TEvent>()), CapacityOf<TEvent>());
            _ringsByEventType[eventType.StructIndex] = ring;
            return ring;
        }
    }
}
