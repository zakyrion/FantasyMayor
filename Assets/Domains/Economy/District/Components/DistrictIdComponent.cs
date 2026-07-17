using System;

namespace Domains.Economy.District.Components
{
    // Primary key of a District entity. Key-role law: a PK type — any other table scoped to a
    // district carries a dedicated …FKComponent, never this type directly (ARCHITECTURE.md → key-role-law).
    public struct DistrictIdComponent : IEquatable<DistrictIdComponent>
    {
        public int Value;

        public bool Equals(DistrictIdComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictIdComponent other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
