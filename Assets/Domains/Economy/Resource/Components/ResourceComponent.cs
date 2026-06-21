using Domains.Economy.Resource.Data;

namespace Domains.Economy.Resource.Components
{
    // Inventory resource stack: one row per (owner, type). The owner is the FK component attached
    // separately to the same entity (CityIdComponent | MayorIdComponent); ResourceTag is the discriminator.
    public struct ResourceComponent
    {
        public ResourceType Type;
        public int Amount;
    }
}
