using System;

namespace Domains.Economy.District.Components
{
    // Primary key of a District entity; the same component is the foreign key carried by anything
    // scoped to a district (e.g. a Building's DistrictId FK).
    public struct DistrictIdComponent : IEquatable<DistrictIdComponent>
    {
        public int Value;

        public bool Equals(DistrictIdComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictIdComponent other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
