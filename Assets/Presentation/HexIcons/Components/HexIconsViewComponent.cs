using Friflo.Engine.ECS;
using Presentation.HexIcons.Views;

namespace Presentation.HexIcons.Components
{
    public readonly struct HexIconsViewComponent : IComponent
    {
        internal readonly HexIconsView View;

        internal HexIconsViewComponent(HexIconsView view)
        {
            View = view;
        }
    }
}
