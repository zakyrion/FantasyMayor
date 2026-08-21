using Domains.Economy.DistrictOpenCondition.Data;
using Friflo.Engine.ECS;

namespace Domains.Economy.DistrictOpenCondition.Components
{
    // State column on a district-open-condition row (Tag Law): whether the gated district is currently
    // buildable. Change-only AddComponent() — re-indexes the ComponentIndex self-index automatically.
    public struct DistrictOpenStateComponent : IIndexedComponent<DistrictOpenState>
    {
        public DistrictOpenState Value;

        public DistrictOpenState GetIndexedValue() => Value;
    }
}
