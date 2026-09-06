using Friflo.Engine.ECS;
using Modules.AxialSystem;

namespace Domains.Map.Hex.Components
{
    public struct HexIdPKComponent : IIndexedComponent<HexCoord>
    {
        public HexCoord Coords;

        public HexCoord GetIndexedValue()
        {
            return Coords;
        }
    }
}
