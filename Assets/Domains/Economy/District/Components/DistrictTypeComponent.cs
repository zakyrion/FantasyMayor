using Domains.Economy.District.Data;
using Friflo.Engine.ECS;

namespace Domains.Economy.District.Components
{
    // Data component: the district-type value on a District FACT row (the entity IS of this type). Rows that
    // merely REFERENCE a district type (open-condition, build-outcome, in-progress build) carry
    // DistrictTypeFKComponent instead — never this type. Indexed so it is a valid ECS key for ComponentIndex
    // (Table Rule; the District table's own self-index).
    public struct DistrictTypeComponent : IIndexedComponent<DistrictType>
    {
        public DistrictType Value;

        public DistrictType GetIndexedValue() => Value;
    }
}
