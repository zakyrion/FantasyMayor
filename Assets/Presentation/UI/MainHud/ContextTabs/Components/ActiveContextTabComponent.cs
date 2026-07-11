using Presentation.UI.MainHud.ContextTabs.Data;

namespace Presentation.UI.MainHud.ContextTabs.Components
{
    /// <summary>
    ///     World-singleton state: which context tab is currently active. Immutable — write a new value via
    ///     <c>World.Set</c> (never ref-mutated). The view records the click here, then a payload-less pulse lets
    ///     ContextTabSelectionSystem reconcile the presentation against it.
    /// </summary>
    public readonly struct ActiveContextTabComponent
    {
        public readonly ContextTab Value;

        public ActiveContextTabComponent(ContextTab value)
        {
            Value = value;
        }
    }
}
