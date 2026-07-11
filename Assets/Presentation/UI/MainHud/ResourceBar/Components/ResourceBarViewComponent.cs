using Presentation.UI.MainHud.ResourceBar.Views;

namespace Presentation.UI.MainHud.ResourceBar.Components
{
    /// <summary>
    ///     Singleton-entity component holding the resolved resource-strip view. The instance lifetime is owned by
    ///     the shared Main UI prefab (MainHudSpawnSystem holds the addressable Box); this only references it.
    /// </summary>
    public readonly struct ResourceBarViewComponent
    {
        public readonly ResourceBarView View;

        public ResourceBarViewComponent(ResourceBarView view)
        {
            View = view;
        }
    }
}
