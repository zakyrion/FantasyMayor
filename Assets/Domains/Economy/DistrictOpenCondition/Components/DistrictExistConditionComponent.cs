using Domains.Economy.District.Data;
using Friflo.Engine.ECS;

namespace Domains.Economy.DistrictOpenCondition.Components
{
    // Payload of a DistrictExistCondition entity: the district type that must already exist for the gated
    // district (carried by the entity's DistrictTypeFKComponent) to become buildable.
    public struct DistrictExistConditionComponent : IComponent
    {
        public DistrictType RequiredDistrict;
    }
}
