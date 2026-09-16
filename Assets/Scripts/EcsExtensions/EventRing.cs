using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     One event type's ring: the archetype its entities are born in, and the circular buffer of live
    ///     slots from the oldest present event to the newest. Lives inside <see cref="EventLog" />; nothing
    ///     outside it reaches into a ring directly.
    /// </summary>
    internal sealed class EventRing
    {
        private readonly EventRingSlot[] _slots;
        private int _head;
        private int _count;
        private long _appended;

        public EventRing(Archetype archetype, int capacity)
        {
            Archetype = archetype;
            _slots = new EventRingSlot[capacity];
        }

        public Archetype Archetype { get; }
        public bool IsFull => _count == _slots.Length;
        public bool IsEmpty => _count == 0;

        /// <summary>The offset of the oldest present event — the offset that has not been born yet is <see cref="AppendedCount" />.</summary>
        public long OldestOffset => _appended - _count;

        /// <summary>How many events of this type were raised over the process.</summary>
        public long AppendedCount => _appended;

        public EventRingSlot Oldest => _slots[_head];

        public void DropOldest()
        {
            _head = (_head + 1) % _slots.Length;
            _count--;
        }

        /// <summary>Places the newest slot. The caller evicts the oldest first when the ring is full — a full ring never reaches here.</summary>
        public void Append(EventRingSlot slot)
        {
            var tailSlot = (_head + _count) % _slots.Length;
            _slots[tailSlot] = slot;
            _count++;
            _appended++;
        }

        public EventRingSlot SlotAt(long offset)
        {
            var distance = (int)(offset - OldestOffset);
            return _slots[(_head + distance) % _slots.Length];
        }
    }
}
