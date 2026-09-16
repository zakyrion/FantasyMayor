using EcsExtensions;
namespace Presentation.Terrain.Events
{
    /// <summary>
    ///     Payload-less event raised by HexSelectionSystem whenever the selection changes — a hex selected,
    ///     re-selected to another coord, or deselected. Consumers reconcile against the current
    ///     HexSelectedComponent (present/value); the event carries no data by design. Lives in the event log
    ///     until evicted past its ring capacity.
    /// </summary>
    public struct SelectedHexChangedEvent : IEventTag
    {
    }
}
