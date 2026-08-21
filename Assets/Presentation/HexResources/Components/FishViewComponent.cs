using Domains.Map.HexResources.Data;
using Friflo.Engine.ECS;
using Presentation.HexResources.Views;

namespace Presentation.HexResources.Components
{
    internal struct FishViewComponent : IComponent
    {
        public HexResourceType Type;
        public FishView View;
    }
}
