using Friflo.Engine.ECS;
using Modules.AxialSystem;
using System;

namespace Domains.Map.Hex.Components
{
    public struct HexIdComponent : IEquatable<HexIdComponent>, IComponent
    {
        public HexCoord Coords;

        public bool Equals(HexIdComponent other) => Coords.Equals(other.Coords);
        public override bool Equals(object obj) => obj is HexIdComponent other && Equals(other);
        public override int GetHashCode() => Coords.GetHashCode();
    }
}
