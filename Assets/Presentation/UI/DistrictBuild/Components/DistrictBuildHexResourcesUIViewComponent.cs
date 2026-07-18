using Friflo.Engine.ECS;
using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Components
{
    // World component: the requirements (hex-resources) section view, resolved off the overlay prefab at spawn.
    public readonly struct DistrictBuildHexResourcesUIViewComponent : IComponent
    {
        public readonly DistrictBuildHexResourcesUIView View;

        public DistrictBuildHexResourcesUIViewComponent(DistrictBuildHexResourcesUIView view)
        {
            View = view;
        }
    }
}
