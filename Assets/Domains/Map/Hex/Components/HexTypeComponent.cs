using Domains.Map.Hex.Data;
using Friflo.Engine.ECS;

namespace Domains.Map.Hex.Components
{
    // Terrain kind of a hex as a single column on the hex row (replaces the data-less terrain tags
    // HexPlainTag / HexMountTag / HexBedhillTag / HexWaterTag). Indexed on the enum so it can serve
    // as a ComponentIndex key — "all hexes of type X" — mirroring HexResourceComponent.
    public struct HexTypeComponent : IIndexedComponent<HexType>
    {
        public HexType Type;

        public HexType GetIndexedValue() => Type;
    }
}
