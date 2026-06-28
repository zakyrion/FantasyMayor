using System;
using DefaultEcs;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Data;
using Domains.Economy.Resource.Tags;

namespace Domains.Economy.Resource.Helpers
{
    // Stateless: creates one inventory-resource stack per ResourceType for a single owner.
    // The owner FK component (CityIdComponent | MayorIdComponent | ...) is attached generically as the
    // SoA owner key; ResourceTag is the table discriminator. Starting amounts may be supplied per type
    // (e.g. flattened from a loaded config); any ResourceType absent from startingAmounts begins at 0.
    // The full per-type loadout is always created regardless of what startingAmounts contains.
    public static class ResourceLoadoutSpawner
    {
        public static void SpawnLoadout<TOwnerId>(World world, in TOwnerId owner,
            ReadOnlySpan<ResourceComponent> startingAmounts = default) where TOwnerId : struct
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                // ActionPoint is NOT part of the generic inventory loadout: only AP owners (Mayor, later
                // Important Citizens) hold an AP stack, seeded explicitly via SpawnResource. The City has none.
                if (type == ResourceType.Unknown || type == ResourceType.ActionPoint)
                    continue;

                SpawnResource(world, owner, type, AmountFor(type, startingAmounts));
            }
        }

        // Creates a single SoA resource stack (owner FK + ResourceComponent + ResourceTag). Use for
        // owner-specific stacks excluded from the generic loadout (e.g. the Mayor's ActionPoint pool).
        public static void SpawnResource<TOwnerId>(World world, in TOwnerId owner, ResourceType type, int amount)
            where TOwnerId : struct
        {
            var entity = world.CreateEntity();
            entity.Set(owner);
            entity.Set(new ResourceComponent { Type = type, Amount = amount });
            entity.Set(new ResourceTag());
        }

        private static int AmountFor(ResourceType type, ReadOnlySpan<ResourceComponent> startingAmounts)
        {
            foreach (var entry in startingAmounts)
                if (entry.Type == type)
                    return entry.Amount;

            return 0;
        }
    }
}
