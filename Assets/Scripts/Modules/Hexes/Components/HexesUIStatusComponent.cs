using Modules.Hexes.View.UI;
using Unity.Entities;

namespace Modules.Hexes.Components
{
    public struct HexesUIStatusComponent : IComponentData
    {
        public bool Active;
        public UnityObjectRef<HexesUIView> ObjectRef;
    }
}
