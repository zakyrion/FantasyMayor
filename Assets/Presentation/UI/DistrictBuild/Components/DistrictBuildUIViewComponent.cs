using Friflo.Engine.ECS;
using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Components
{
    /// <summary>
    ///     Singleton-entity component holding the district-build overlay view. The instance lifetime (addressable
    ///     Box) is owned by DistrictBuildUISpawnSystem via <see cref="DistrictBuildUIRootComponent" />;
    ///     this only references the view. Mirrors TurnPanelViewComponent.
    /// </summary>
    public readonly struct DistrictBuildUIViewComponent : IComponent
    {
        public readonly DistrictBuildUIView View;

        public DistrictBuildUIViewComponent(DistrictBuildUIView view)
        {
            View = view;
        }
    }
}
