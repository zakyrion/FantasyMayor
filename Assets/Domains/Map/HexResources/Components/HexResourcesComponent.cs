using System;
using Domains.Map.HexResources.Data;

namespace Domains.Map.HexResources.Components
{
    public struct HexResourcesComponent : IEquatable<HexResourcesComponent>
    {
        public ResourceType Type;

        public bool Equals(HexResourcesComponent other) => Type == other.Type;
        public override bool Equals(object obj) => obj is HexResourcesComponent other && Equals(other);
        public override int GetHashCode() => (int)Type;
    }
}
