using Modules.HexIcons.Views;

namespace Modules.HexIcons.Components
{
    internal readonly struct HexIconsViewComponent
    {
        public readonly HexIconsView View;

        internal HexIconsViewComponent(HexIconsView view)
        {
            View = view;
        }
    }
}
