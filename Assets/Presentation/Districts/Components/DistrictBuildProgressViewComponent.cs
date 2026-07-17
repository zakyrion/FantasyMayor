using Domains.Economy.District.Data;
using Presentation.Districts.Views;

namespace Presentation.Districts.Components
{
    // One spawned construction-progress view: the district type and the view MonoBehaviour reference, so the
    // GameObject can be destroyed when the build completes or is cancelled. Keyed back to its hex by the sibling
    // HexIdFKComponent on the same view entity.
    internal struct DistrictBuildProgressViewComponent
    {
        public DistrictType Type;
        public DistrictBuildProgressView View;
    }
}
