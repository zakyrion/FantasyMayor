using System;
using Domains.Map.HexResources.Data;

namespace Domains.Map.HexResources.Components
{
    public struct HexResourceComponent : IEquatable<HexResourceComponent>
    {
        public HexResourceType Type;

        public bool Equals(HexResourceComponent other) => Type == other.Type;
        public override bool Equals(object obj) => obj is HexResourceComponent other && Equals(other);
        public override int GetHashCode() => (int)Type;
    }
}
