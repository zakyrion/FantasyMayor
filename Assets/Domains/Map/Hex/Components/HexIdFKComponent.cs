using System;
using Modules.AxialSystem;

namespace Domains.Map.Hex.Components
{
    // FK into the Hex key space: carried by rows of other tables, keys their AsMultiMap indexes.
    public struct HexIdFKComponent : IEquatable<HexIdFKComponent>
    {
        public HexCoord Coords;

        public bool Equals(HexIdFKComponent other) => Coords.Equals(other.Coords);
        public override bool Equals(object obj) => obj is HexIdFKComponent other && Equals(other);
        public override int GetHashCode() => Coords.GetHashCode();
    }
}
