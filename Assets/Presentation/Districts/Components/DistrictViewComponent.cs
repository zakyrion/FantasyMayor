using Domains.Economy.District.Data;
using Friflo.Engine.ECS;
using Presentation.Districts.Views;

namespace Presentation.Districts.Components
{
    // One spawned district view: the district type and the view MonoBehaviour reference. The reference is held
    // so the DistrictView GameObject can be managed (e.g. destroyed) when the district is later removed. Keyed
    // back to its hex by the sibling HexIdFKComponent on the same view entity.
    internal struct DistrictViewComponent : IComponent
    {
        public DistrictType Type;
        public DistrictView View;
    }
}
