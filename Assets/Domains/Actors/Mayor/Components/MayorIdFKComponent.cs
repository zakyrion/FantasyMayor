using System;

namespace Domains.Actors.Mayor.Components
{
    // FK into the Mayor key space: carried by rows of other tables, keys their AsMultiMap indexes.
    public struct MayorIdFKComponent : IEquatable<MayorIdFKComponent>
    {
        public int Value;

        public bool Equals(MayorIdFKComponent other) => Value == other.Value;
        public override bool Equals(object obj) => obj is MayorIdFKComponent other && Equals(other);
        public override int GetHashCode() => Value;
    }
}
