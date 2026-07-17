using System;
using Domains.Economy.DistrictOpenCondition.Data;

namespace Domains.Economy.DistrictOpenCondition.Components
{
    // State column on a district-open-condition row (Tag Law): whether the gated district is currently
    // buildable. Change-only Set() — re-indexes the AsMultiMap self-index automatically.
    public struct DistrictOpenStateComponent : IEquatable<DistrictOpenStateComponent>
    {
        public DistrictOpenState Value;

        public bool Equals(DistrictOpenStateComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictOpenStateComponent other && Equals(other);
        public override int GetHashCode() => (int)Value;
    }
}
