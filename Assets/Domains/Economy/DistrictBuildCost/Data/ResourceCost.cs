using System;
using Domains.Economy.Resource.Data;

namespace Domains.Economy.DistrictBuildCost.Data
{
    // Authored/value alias of a district-build price entry (ResourceType + amount). Structurally identical to
    // ResourceComponent / ResourceAmount but a DISTINCT type by role: the per-district cost-catalogue value,
    // never entity state and never an inventory amount. Mapped to ResourceAmount by explicit field assignment
    // at the commit boundary before it reaches the spend pipeline.
    [Serializable]
    public struct ResourceCost
    {
        public ResourceType Type;
        public int Amount;
    }
}
