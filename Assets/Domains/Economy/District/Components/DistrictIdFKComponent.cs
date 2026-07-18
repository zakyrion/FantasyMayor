using System;

namespace Domains.Economy.District.Components
{
    // FK into the District PK space: carried by rows of other tables, keys their AsMultiMap indexes.
    public struct DistrictIdFKComponent : IEquatable<DistrictIdFKComponent>
    {
        public int Value;

        public bool Equals(DistrictIdFKComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictIdFKComponent other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
