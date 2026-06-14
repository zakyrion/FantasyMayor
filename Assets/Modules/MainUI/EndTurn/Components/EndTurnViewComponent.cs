using Modules.MainUI.EndTurn.Views;

namespace Modules.MainUI.EndTurn.Components
{
    /// <summary>
    ///     Singleton-entity component holding the loaded end-turn button view. The instance lifetime is owned
    ///     by EndTurnSpawnSubSystem (it holds the addressable Box); this only references it. Mirrors
    ///     HexInfoPanelViewComponent.
    /// </summary>
    public readonly struct EndTurnViewComponent
    {
        public readonly EndTurnView View;

        public EndTurnViewComponent(EndTurnView view)
        {
            View = view;
        }
    }
}
