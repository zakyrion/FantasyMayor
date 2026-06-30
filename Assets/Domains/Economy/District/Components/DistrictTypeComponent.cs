using System;
using Domains.Economy.District.Data;

namespace Domains.Economy.District.Components
{
    // Shared district-type vocabulary component. The same component is the catalogue key on district-type
    // tables and the foreign key carried by anything scoped to a district TYPE (e.g. a district-open-condition
    // entity). IEquatable so it is a valid ECS key for AsMap/AsMultiMap (Table Rule).
    public struct DistrictTypeComponent : IEquatable<DistrictTypeComponent>
    {
        public DistrictType Value;

        public bool Equals(DistrictTypeComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictTypeComponent other && Equals(other);
        public override int GetHashCode() => (int)Value;
    }
}
