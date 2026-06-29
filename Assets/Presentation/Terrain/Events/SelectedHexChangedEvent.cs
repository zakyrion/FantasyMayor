namespace Presentation.Terrain.Events
{
    /// <summary>
    ///     Payload-less one-frame pulse raised by HexSelectionSystem whenever the selection changes — a hex
    ///     selected, re-selected to another coord, or deselected. Consumers reconcile against the current
    ///     HexSelectedComponent (present/value); the pulse carries no data by design. Paired with EventTag;
    ///     disposed each tick by EventCleanupSystem.
    /// </summary>
    public struct SelectedHexChangedEvent
    {
    }
}
