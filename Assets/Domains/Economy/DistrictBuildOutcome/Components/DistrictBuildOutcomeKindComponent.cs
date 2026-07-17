using System;
using Domains.Economy.DistrictBuildOutcome.Data;

namespace Domains.Economy.DistrictBuildOutcome.Components
{
    // Kind column on a district-build-outcome row (Tag Law): which outcome effect the row applies on completion.
    public struct DistrictBuildOutcomeKindComponent : IEquatable<DistrictBuildOutcomeKindComponent>
    {
        public DistrictBuildOutcomeKind Value;

        public bool Equals(DistrictBuildOutcomeKindComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictBuildOutcomeKindComponent other && Equals(other);
        public override int GetHashCode() => (int)Value;
    }
}
