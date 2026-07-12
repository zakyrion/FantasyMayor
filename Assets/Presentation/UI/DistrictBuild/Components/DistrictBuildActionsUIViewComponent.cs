using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Components
{
    // World component: the actions section view (dormant scaffold), resolved off the overlay prefab at spawn.
    public readonly struct DistrictBuildActionsUIViewComponent
    {
        public readonly DistrictBuildActionsUIView View;

        public DistrictBuildActionsUIViewComponent(DistrictBuildActionsUIView view)
        {
            View = view;
        }
    }
}
