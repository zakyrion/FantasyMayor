using System;

namespace Core.Data
{
    public readonly struct SubscriptionId : IEquatable<SubscriptionId>
    {
        public Guid Id { get; }

        public SubscriptionId(Guid id)
        {
            Id = id;
        }

        public bool Equals(SubscriptionId other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            return obj is SubscriptionId o && Equals(o);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
