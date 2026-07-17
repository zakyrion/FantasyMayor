using System;
using Domains.Economy.DistrictOpenCondition.Data;

namespace Domains.Economy.DistrictOpenCondition.Components
{
    // Kind column on a district-open-condition row (Tag Law): which condition rule the row evaluates.
    public struct DistrictOpenConditionKindComponent : IEquatable<DistrictOpenConditionKindComponent>
    {
        public DistrictOpenConditionKind Value;

        public bool Equals(DistrictOpenConditionKindComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictOpenConditionKindComponent other && Equals(other);
        public override int GetHashCode() => (int)Value;
    }
}
