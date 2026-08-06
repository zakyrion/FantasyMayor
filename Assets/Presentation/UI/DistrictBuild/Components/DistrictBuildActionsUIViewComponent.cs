using Friflo.Engine.ECS;
using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Components
{
    // Singleton component: the actions section view (dormant scaffold), resolved off the overlay prefab at spawn.
    public readonly struct DistrictBuildActionsUIViewComponent : IComponent
    {
        public readonly DistrictBuildActionsUIView View;

        public DistrictBuildActionsUIViewComponent(DistrictBuildActionsUIView view)
        {
            View = view;
        }
    }
}
