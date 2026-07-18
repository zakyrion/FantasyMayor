using Friflo.Engine.ECS;
using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Components
{
    // World component: the district-list section view, resolved off the overlay prefab at spawn. Mirrors
    // DistrictBuildUIViewComponent in shape, but is a world component so DistrictBuildListUISubSystem reads it
    // directly via World.Get.
    public readonly struct DistrictBuildListUIViewComponent : IComponent
    {
        public readonly DistrictBuildListUIView View;

        public DistrictBuildListUIViewComponent(DistrictBuildListUIView view)
        {
            View = view;
        }
    }
}
