using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Components
{
    // World component: the price (cost + payer) section view, resolved off the overlay prefab at spawn.
    public readonly struct DistrictBuildPriceUIViewComponent
    {
        public readonly DistrictBuildPriceUIView View;

        public DistrictBuildPriceUIViewComponent(DistrictBuildPriceUIView view)
        {
            View = view;
        }
    }
}
