using Domains.Economy.District.Data;

namespace Domains.Economy.DistrictOpenCondition.Components
{
    // Payload of a DistrictExistCondition entity: the district type that must already exist for the gated
    // district (carried by the entity's DistrictTypeComponent FK) to become buildable.
    public struct DistrictExistConditionComponent
    {
        public DistrictType RequiredDistrict;
    }
}
