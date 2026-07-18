using Domains.Map.HexResources.Data;
using Friflo.Engine.ECS;

namespace Domains.Map.HexResources.Components
{
    public struct HexResourceComponent : IIndexedComponent<HexResourceType>
    {
        public HexResourceType Type;

        public HexResourceType GetIndexedValue() => Type;
    }
}
