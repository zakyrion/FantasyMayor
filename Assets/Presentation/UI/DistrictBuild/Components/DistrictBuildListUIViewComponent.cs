using Friflo.Engine.ECS;
using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Components
{
    // Singleton component: the district-list section view, resolved off the overlay prefab at spawn. Mirrors
    // DistrictBuildUIViewComponent in shape, but is a singleton component so DistrictBuildListUISubSystem reads it
    // directly via Singletons.Get.
    public readonly struct DistrictBuildListUIViewComponent : IComponent
    {
        public readonly DistrictBuildListUIView View;

        public DistrictBuildListUIViewComponent(DistrictBuildListUIView view)
        {
            View = view;
        }
    }
}
