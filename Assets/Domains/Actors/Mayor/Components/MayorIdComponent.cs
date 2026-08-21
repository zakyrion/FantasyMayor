using Friflo.Engine.ECS;
using System;

namespace Domains.Actors.Mayor.Components
{
    // Primary key of the Mayor entity.
    public struct MayorIdComponent : IEquatable<MayorIdComponent>, IComponent
    {
        public int Value;

        public bool Equals(MayorIdComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is MayorIdComponent other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
