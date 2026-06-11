using System;
using Modules.AxialSystem;

namespace Modules.HexCore.Components
{
    public struct HexIdComponent : IEquatable<HexIdComponent>
    {
        public HexCoord Coords;

        public bool Equals(HexIdComponent other) => Coords.Equals(other.Coords);
        public override bool Equals(object obj) => obj is HexIdComponent other && Equals(other);
        public override int GetHashCode() => Coords.GetHashCode();
    }
}
