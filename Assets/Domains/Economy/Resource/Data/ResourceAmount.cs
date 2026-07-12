using System;

namespace Domains.Economy.Resource.Data
{
    // Authored/value alias of an inventory resource quantity (ResourceType + amount). Structurally identical to
    // ResourceComponent but a DISTINCT type by role: this is config/authored/spend-list data, never entity state.
    // ResourceComponent is the ECS component living on a resource-stack entity; the two never mix — convert by
    // explicit field assignment at the boundary (config→spawn, cost→spend).
    [Serializable]
    public struct ResourceAmount
    {
        public ResourceType Type;
        public int Amount;
    }
}
