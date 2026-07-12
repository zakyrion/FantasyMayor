using System;
using Domains.Kernel.Data;

namespace Domains.Actors.Components
{
    public struct ActorTypeComponent : IEquatable<ActorTypeComponent>
    {
        public ActorType Type;

        public bool Equals(ActorTypeComponent other)
        {
            return Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            return obj is ActorTypeComponent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)Type;
        }
    }
}
