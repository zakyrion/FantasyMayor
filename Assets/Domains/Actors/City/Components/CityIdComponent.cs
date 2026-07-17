using System;

namespace Domains.Actors.City.Components
{
    // Primary key of a City entity.
    public struct CityIdComponent : IEquatable<CityIdComponent>
    {
        public int Value;

        public bool Equals(CityIdComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CityIdComponent other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
