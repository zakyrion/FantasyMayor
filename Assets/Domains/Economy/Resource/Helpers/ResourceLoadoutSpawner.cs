using System;
using DefaultEcs;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Data;

namespace Domains.Economy.Resource.Helpers
{
    // Stateless: creates one inventory-resource stack per ResourceType for a single owner.
    // The owner FK component (CityIdFKComponent | MayorIdFKComponent | ...) is attached generically as the
    // SoA owner key; TResourceTag is the owner-scoped table discriminator (CityResourceTag |
    // MayorResourceTag | ...) — each owner kind has its OWN resource table (Tag Law), this helper knows
    // none of them. Starting amounts may be supplied per type (e.g. flattened from a loaded config);
    // any ResourceType absent from startingAmounts begins at 0. The full per-type loadout is always
    // created regardless of what startingAmounts contains.
    public static class ResourceLoadoutSpawner
    {
        public static void SpawnLoadout<TOwnerId, TResourceTag>(World world, in TOwnerId owner,
            ReadOnlySpan<ResourceAmount> startingAmounts = default)
            where TOwnerId : struct
            where TResourceTag : struct
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                // Unknown is the zero sentinel, never a real stack.
                if (type == ResourceType.Unknown)
                    continue;

                SpawnResource<TOwnerId, TResourceTag>(world, owner, type, AmountFor(type, startingAmounts));
            }
        }

        // Creates a single SoA resource stack (owner FK + ResourceComponent + owner-scoped resource tag).
        public static void SpawnResource<TOwnerId, TResourceTag>(World world, in TOwnerId owner,
            ResourceType type, int amount)
            where TOwnerId : struct
            where TResourceTag : struct
        {
            var entity = world.CreateEntity();
            entity.Set(owner);
            entity.Set(new ResourceComponent { Type = type, Amount = amount });
            entity.Set(new TResourceTag());
        }

        private static int AmountFor(ResourceType type, ReadOnlySpan<ResourceAmount> startingAmounts)
        {
            foreach (var entry in startingAmounts)
                if (entry.Type == type)
                    return entry.Amount;

            return 0;
        }
    }
}
