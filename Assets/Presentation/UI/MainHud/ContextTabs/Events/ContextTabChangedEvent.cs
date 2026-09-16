using EcsExtensions;
namespace Presentation.UI.MainHud.ContextTabs.Events
{
    /// <summary>
    ///     Payload-less event raised by ContextTabsView after it writes the new ActiveContextTabComponent on a
    ///     tab click. ContextTabSelectionSystem reacts to it and reconciles the view against the current
    ///     active-tab state (idempotent) — the event carries no data by design. Lives in the event log until
    ///     evicted past its ring capacity.
    /// </summary>
    public struct ContextTabChangedEvent : IEventTag
    {
    }
}
