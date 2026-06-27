using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Components
{
    /// <summary>
    ///     Singleton-entity component holding the district-build overlay view. The instance lifetime (addressable
    ///     Box) is owned by DistrictBuildActionSpawnSystem via <see cref="DistrictBuildActionRootComponent" />;
    ///     this only references the view. Mirrors EndTurnViewComponent.
    /// </summary>
    public readonly struct DistrictBuildActionViewComponent
    {
        public readonly DistrictBuildActionView View;

        public DistrictBuildActionViewComponent(DistrictBuildActionView view)
        {
            View = view;
        }
    }
}
