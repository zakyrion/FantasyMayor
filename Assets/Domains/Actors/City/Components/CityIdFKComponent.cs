using System;

namespace Domains.Actors.City.Components
{
    // FK into the City key space: carried by rows of other tables, keys their AsMultiMap indexes.
    public struct CityIdFKComponent : IEquatable<CityIdFKComponent>
    {
        public int Value;

        public bool Equals(CityIdFKComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CityIdFKComponent other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
