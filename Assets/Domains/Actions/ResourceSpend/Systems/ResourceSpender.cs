using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using Domains.Actors.Data;
using Domains.Economy.Resource.Data;
using Domains.Economy.Resource.Helpers;
using JetBrains.Annotations;

namespace Domains.Actions.ResourceSpend.Systems
{
    // Orchestrator (routing variant) for a district-build spend. NOT an ECS system — a service the commit system
    // calls. AP is always paid by the mayor (the sole Action-Point pool); resources are paid by the chosen payer
    // (City or Mayor). Affordability is checked for BOTH before either is deducted (atomic); an unaffordable or
    // unroutable spend fails loud and deducts nothing. Owner-blind arithmetic is delegated to Economy's ResourceLedger.
    [UsedImplicitly]
    public sealed class ResourceSpender
    {
        private readonly IReadOnlyList<ResourceSpendSubSystem> _subSystems;
        private readonly ResourceSpendSubSystem _actionPointOwner;

        public ResourceSpender(IReadOnlyList<ResourceSpendSubSystem> subSystems)
        {
            _subSystems = subSystems;
            _actionPointOwner = subSystems.SingleOrDefault(subSystem => subSystem.HandlesActionPoints)
                ?? throw new InvalidOperationException("No resource-spend subsystem owns an Action-Point pool.");
        }

        public void Spend(ActorType payerType, int payerId, int actionPoints, ReadOnlySpan<ResourceAmount> resourcePrices)
        {
            var payer = ResolvePayer(payerType);

            if (!payer.TryGetStacks(payerId, out var payerStacks))
                throw new InvalidOperationException($"No resource stacks for payer {payerType}#{payerId}.");
            if (!_actionPointOwner.TryGetActionPointStacks(out var actionPointStacks))
                throw new InvalidOperationException("No Action-Point stacks for the mayor.");

            var affordable =ResourceLedger.CanAfford(payerStacks, resourcePrices);
            if (!affordable)
                throw new InvalidOperationException(
                    $"Unaffordable district build for {payerType}#{payerId}: needs {actionPoints} AP plus the listed resources.");

            ResourceLedger.Deduct(payerStacks, resourcePrices);
        }

        private ResourceSpendSubSystem ResolvePayer(ActorType payerType)
        {
            foreach (var subSystem in _subSystems)
                if (subSystem.Owner == payerType)
                    return subSystem;

            throw new InvalidOperationException($"No resource-spend subsystem for owner {payerType}.");
        }
    }
}
