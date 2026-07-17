using System;
using Domains.Economy.District.Data;

namespace Domains.Economy.District.Components
{
    // FK into the District-type key space: carried by rows of other tables, keys their AsMultiMap indexes.
    public struct DistrictTypeFKComponent : IEquatable<DistrictTypeFKComponent>
    {
        public DistrictType Value;

        public bool Equals(DistrictTypeFKComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is DistrictTypeFKComponent other && Equals(other);
        public override int GetHashCode() => (int)Value;
    }
}
