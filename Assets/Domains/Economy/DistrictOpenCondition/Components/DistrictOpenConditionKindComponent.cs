using Domains.Economy.DistrictOpenCondition.Data;
using Friflo.Engine.ECS;

namespace Domains.Economy.DistrictOpenCondition.Components
{
    // Kind column on a district-open-condition row (Tag Law): which condition rule the row evaluates.
    public struct DistrictOpenConditionKindComponent : IIndexedComponent<DistrictOpenConditionKind>
    {
        public DistrictOpenConditionKind Value;

        public DistrictOpenConditionKind GetIndexedValue() => Value;
    }
}
