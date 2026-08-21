using Friflo.Engine.ECS;
using Presentation.UI.MainHud.TurnPanel.Views;

namespace Presentation.UI.MainHud.TurnPanel.Components
{
    /// <summary>
    ///     Singleton-entity component holding the loaded turn-panel view. The instance lifetime is owned
    ///     by TurnPanelSpawnSubSystem (it holds the addressable Box); this only references it. Mirrors
    ///     HexInfoPanelViewComponent.
    /// </summary>
    public readonly struct TurnPanelViewComponent : IComponent
    {
        public readonly TurnPanelView View;

        public TurnPanelViewComponent(TurnPanelView view)
        {
            View = view;
        }
    }
}
