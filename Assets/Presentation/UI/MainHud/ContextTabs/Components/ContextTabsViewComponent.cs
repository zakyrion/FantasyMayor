using Friflo.Engine.ECS;
using Presentation.UI.MainHud.ContextTabs.Views;

namespace Presentation.UI.MainHud.ContextTabs.Components
{
    /// <summary>
    ///     Singleton component referencing the context-tabs view resolved off the shared Main UI instance
    ///     (written via <c>Singletons.Set</c>, like ActiveContextTabComponent / TurnCountComponent — this window has
    ///     no singleton entity). The instance lifetime is the Main UI prefab's; this only references it.
    /// </summary>
    public readonly struct ContextTabsViewComponent : IComponent
    {
        public readonly ContextTabsView View;

        public ContextTabsViewComponent(ContextTabsView view)
        {
            View = view;
        }
    }
}
