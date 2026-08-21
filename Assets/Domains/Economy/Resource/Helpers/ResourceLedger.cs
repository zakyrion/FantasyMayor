using System;
using Friflo.Engine.ECS;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Data;

namespace Domains.Economy.Resource.Helpers
{
    // Owner-agnostic spend/refund math over a set of resource stacks (each stack = one (owner, ResourceType)
    // entity carrying ResourceComponent). The spend/refund counterpart of ResourceLoadoutSpawner: pure, stateless,
    // owner-blind — the caller resolves an owner's stacks and hands them here. Stays in Economy (references no
    // Actor types) so the domain keeps its "no actor dependency" rule; who owns which stacks is sequenced one
    // layer up (Actions).
    public static class ResourceLedger
    {
        // A price entry with Amount <= 0 is skipped: trivially affordable and a no-op to deduct.
        public static bool CanAfford(Entities stacks, ReadOnlySpan<ResourceAmount> prices)
        {
            foreach (var price in prices)
                if (!CanAfford(stacks, price))
                    return false;
            return true;
        }

        public static bool CanAfford(Entities stacks, ResourceAmount price)
        {
            if (price.Amount <= 0)
                return true;
            return TryFindStack(stacks, price.Type, out var stack) && stack.GetComponent<ResourceComponent>().Amount >= price.Amount;
        }

        public static void Deduct(Entities stacks, ReadOnlySpan<ResourceAmount> prices)
        {
            foreach (var price in prices)
                Deduct(stacks, price);
        }

        public static void Deduct(Entities stacks, ResourceAmount price)
        {
            if (price.Amount <= 0)
                return;
            if (!TryFindStack(stacks, price.Type, out var stack))
                throw new InvalidOperationException($"No {price.Type} stack to deduct {price.Amount} from.");

            var current = stack.GetComponent<ResourceComponent>();
            stack.AddComponent(new ResourceComponent { Type = current.Type, Amount = current.Amount - price.Amount });
        }

        // Refund counterpart of Deduct — same all-or-nothing shape, opposite sign. An amount entry with
        // Amount <= 0 is skipped, same as Deduct.
        public static void Credit(Entities stacks, ReadOnlySpan<ResourceAmount> amounts)
        {
            foreach (var amount in amounts)
                Credit(stacks, amount);
        }

        public static void Credit(Entities stacks, ResourceAmount amount)
        {
            if (amount.Amount <= 0)
                return;
            if (!TryFindStack(stacks, amount.Type, out var stack))
                throw new InvalidOperationException($"No {amount.Type} stack to credit {amount.Amount} to.");

            var current = stack.GetComponent<ResourceComponent>();
            stack.AddComponent(new ResourceComponent { Type = current.Type, Amount = current.Amount + amount.Amount });
        }

        private static bool TryFindStack(Entities stacks, ResourceType type, out Entity stack)
        {
            foreach (var candidate in stacks)
                if (candidate.GetComponent<ResourceComponent>().Type == type)
                {
                    stack = candidate;
                    return true;
                }

            stack = default;
            return false;
        }
    }
}
