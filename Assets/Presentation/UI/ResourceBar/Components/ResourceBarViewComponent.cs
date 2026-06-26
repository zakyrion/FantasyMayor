using Presentation.UI.ResourceBar.Views;

namespace Presentation.UI.ResourceBar.Components
{
    /// <summary>
    ///     Singleton-entity component holding the resolved resource-strip view. The instance lifetime is owned by
    ///     the shared Main UI prefab (MainUISpawnSystem holds the addressable Box); this only references it.
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
