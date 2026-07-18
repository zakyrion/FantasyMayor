using Friflo.Engine.ECS;
namespace Presentation.UI.MainHud.ContextTabs.Events
{
    /// <summary>
    ///     Payload-less one-frame pulse raised by ContextTabsView after it writes the new ActiveContextTabComponent
    ///     on a tab click. ContextTabSelectionSystem reacts to it and reconciles the view against the current
    ///     active-tab state (idempotent) — the pulse carries no data by design. Paired with EventTag; disposed
    ///     each tick by EventCleanupSystem.
    /// </summary>
    public struct ContextTabChangedEvent : IComponent
    {
    }
}
