using Friflo.Engine.ECS;
using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Components
{
    // Singleton component: the price (cost + payer) section view, resolved off the overlay prefab at spawn.
    public readonly struct DistrictBuildPriceUIViewComponent : IComponent
    {
        public readonly DistrictBuildPriceUIView View;

        public DistrictBuildPriceUIViewComponent(DistrictBuildPriceUIView view)
        {
            View = view;
        }
    }
}
