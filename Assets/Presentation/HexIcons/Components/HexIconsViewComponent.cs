using Friflo.Engine.ECS;
using Presentation.HexIcons.Views;

namespace Presentation.HexIcons.Components
{
    internal readonly struct HexIconsViewComponent : IComponent
    {
        public readonly HexIconsView View;

        internal HexIconsViewComponent(HexIconsView view)
        {
            View = view;
        }
    }
}
