using System;
using Domains.Economy.District.Data;

namespace Domains.Economy.District.Components
{
    // Data component: the district-type value on a District FACT row (the entity IS of this type). Rows that
    // merely REFERENCE a district type (open-condition, build-outcome, in-progress build) carry
    // DistrictTypeFKComponent instead — never this type. IEquatable so it is a valid ECS key for AsMap/AsMultiMap
    // (Table Rule; the District table's own self-index).
    public struct DistrictTypeComponent : IEquatable<DistrictTypeComponent>
    {
        public DistrictType Value;

        public bool Equals(DistrictTypeComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictTypeComponent other && Equals(other);
        public override int GetHashCode() => (int)Value;
    }
}
