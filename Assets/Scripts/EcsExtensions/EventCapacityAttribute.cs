using System;

namespace EcsExtensions
{
    /// <summary>
    ///     Declares the ring capacity of an event type: a new event past this limit evicts the oldest one of
    ///     its type in <see cref="EventLog" />. A type carrying no attribute gets <see cref="DefaultCapacity" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class EventCapacityAttribute : Attribute
    {
        public const int DefaultCapacity = 128;

        public int Capacity { get; }

        public EventCapacityAttribute(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity,
                    "Event ring capacity must be at least 1.");

            Capacity = capacity;
        }
    }
}
