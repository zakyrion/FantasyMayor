using Modules.AxialSystem;

namespace Modules.MainUI.HexInfoPanel.Events
{
    /// <summary>
    ///     One-frame signal raised by HexInfoPanelSystem when the selected hex changes. Per-block systems
    ///     react to it (anchored on its presence) and rebuild their block for <see cref="Coords" />.
    ///     Paired with EventTag; disposed each tick by EventCleanupSystem.
    /// </summary>
    public struct HexInfoPanelRefreshEvent
    {
        public HexCoord Coords;
    }
}
