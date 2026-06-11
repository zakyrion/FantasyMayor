using Modules.HexesUI.Views;

namespace Modules.HexesUI.Components
{
    /// <summary>
    ///     Singleton-entity component holding the loaded hex info panel view. The instance lifetime is owned
    ///     by HexInfoPanelSpawnSystem (it holds the addressable Box); this only references it.
    /// </summary>
    public readonly struct HexInfoPanelViewComponent
    {
        public readonly HexInfoPanelView View;

        public HexInfoPanelViewComponent(HexInfoPanelView view)
        {
            View = view;
        }
    }
}
