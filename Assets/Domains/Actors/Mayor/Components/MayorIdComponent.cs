using System;

namespace Domains.Actors.Mayor.Components
{
    // Primary key of the Mayor entity; the same component is the foreign key carried by anything the mayor owns.
    public struct MayorIdComponent : IEquatable<MayorIdComponent>
    {
        public int Value;

        public bool Equals(MayorIdComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is MayorIdComponent other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
