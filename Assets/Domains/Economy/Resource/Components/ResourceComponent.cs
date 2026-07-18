using Domains.Economy.Resource.Data;
using Friflo.Engine.ECS;
using System;

namespace Domains.Economy.Resource.Components
{
    // Inventory resource stack: one row per (owner, type). The owner is the FK component attached
    // separately to the same entity (CityIdFKComponent | MayorIdFKComponent); the owner-scoped resource tag
    // (CityResourceTag | MayorResourceTag) is the table discriminator.
    [Serializable]
    public struct ResourceComponent : IComponent
    {
        public ResourceType Type;
        public int Amount;
    }
}
