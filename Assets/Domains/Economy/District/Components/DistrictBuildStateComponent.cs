using Domains.Economy.District.Data;
using Friflo.Engine.ECS;
using System;

namespace Domains.Economy.District.Components
{
    // Stage column on the District row (Tag Law): Planned from CONFIRM, Built at completion. Change-only Set()
    // — re-indexes the AsMultiMap self-index automatically.
    public struct DistrictBuildStateComponent : IEquatable<DistrictBuildStateComponent>, IComponent
    {
        public DistrictBuildState Value;

        public bool Equals(DistrictBuildStateComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictBuildStateComponent other && Equals(other);
        public override int GetHashCode() => (int)Value;
    }
}
