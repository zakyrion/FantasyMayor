using Domains.Kernel.Data;
using Friflo.Engine.ECS;
using System;

namespace Domains.Actors.Components
{
    public struct ActorTypeComponent : IEquatable<ActorTypeComponent>, IComponent
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
