using System;
using Domains.Economy.Resource.Data;

namespace Domains.Economy.Resource.Components
{
    public struct ResourceTypeComponent : IEquatable<ResourceTypeComponent>
    {
        public ResourceType ResourceType;

        public bool Equals(ResourceTypeComponent other)
        {
            return ResourceType == other.ResourceType;
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceTypeComponent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)ResourceType;
        }
    }
}
