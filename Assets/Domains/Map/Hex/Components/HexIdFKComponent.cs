using Friflo.Engine.ECS;
using Modules.AxialSystem;

namespace Domains.Map.Hex.Components
{
    // FK into the Hex key space: carried by rows of other tables, keys their ComponentIndex.
    public struct HexIdFKComponent : IIndexedComponent<HexCoord>
    {
        public HexCoord Coords;

        public HexCoord GetIndexedValue() => Coords;
    }
}
