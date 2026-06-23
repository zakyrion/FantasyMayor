using System;
using Domains.Map.Hex.Data;

namespace Domains.Map.Hex.Components
{
    // Terrain kind of a hex as a single column on the hex row (replaces the data-less terrain tags
    // HexPlainTag / HexMountTag / HexBedhillTag / HexWaterTag). IEquatable on the enum so it can serve
    // as an AsMultiMap key — "all hexes of type X" — mirroring HexResourcesComponent.
    public struct HexTypeComponent : IEquatable<HexTypeComponent>
    {
        public HexType Type;

        public bool Equals(HexTypeComponent other) => Type == other.Type;
        public override bool Equals(object obj) => obj is HexTypeComponent other && Equals(other);
        public override int GetHashCode() => (int)Type;
    }
}
