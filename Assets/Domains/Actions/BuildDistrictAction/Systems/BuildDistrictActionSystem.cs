using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Actions.Components;
using Domains.Actors.City.Components;
using Domains.Actors.City.Tags;
using Domains.Actors.Components;
using Domains.Actors.Mayor.Components;
using Domains.Actors.Mayor.Tags;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Helpers;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Economy.DistrictBuildCost.Configs;
using Domains.Economy.Resource.Data;
using Domains.Economy.Resource.Helpers;
using Domains.Kernel.Data;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Reactive: on the <see cref="DistrictBuildConfirmedEvent" /> pulse SPENDS the district's price then creates
    ///     the IN-PROGRESS build entity. Spend (R2) runs first and all-or-nothing: the payer's resource stockpile is
    ///     charged the <c>DistrictBuildCostConfig.DistrictPrices</c> (via <c>ResourceLedger</c>) and the Mayor's
    ///     <c>MayorAPComponent</c> pool is charged the <c>ApPrice</c> (AP is always Mayor-paid). Affordability is
    ///     checked across the WHOLE price before any deduction, and a shortfall throws — the confirm is UI-gated
    ///     (<c>DistrictBuildPriceUISubSystem</c> disables «Збудувати» when unaffordable), so a shortfall here is a
    ///     broken invariant, not a normal path. Only after a successful spend is the entity stamped with
    ///     <c>HexIdComponent</c> + <c>DistrictTypeComponent</c> from the pulse, a unique <c>ActionIdComponent</c>
    ///     (shared <c>ActionIdAllocatorComponent</c> counter, seeded here), the turn countdown
    ///     (<c>BuildDistrictTurnsComponent</c> = the district's <c>TurnsToBuild</c>, twice), the chosen payer
    ///     (<c>ActorTypeComponent</c>), and <c>BuildDistrictInProgressTag</c>. There is no draft: the build is
    ///     committed directly on confirm. Hex, district, and payer come from the pulse (the Actions assembly can't read
    ///     the Presentation selection). <c>BuildDistrictTurnTickSystem</c> counts the entity down each turn and
    ///     <c>BuildDistrictCompletionSystem</c> materialises the District fact at zero. See
    ///     <c>Patterns/PATTERN_REACTIVE_SYSTEM.md</c> and <c>Flows/FLOW_DISTRICT_BUILD.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictActionSystem : UpdatedSystem
    {
        private readonly World _world;

        // Actor rows (Table Rule): id PK + tag + ActorTypeComponent discriminator; the Mayor also carries the AP pool.
        private readonly EntitySet _mayorActor;
        private readonly EntitySet _cityActor;

        // Resource stacks grouped by owner id — the payer's stockpile handed to ResourceLedger for the spend.
        private readonly EntityMultiMap<MayorIdComponent> _mayorResources;
        private readonly EntityMultiMap<CityIdComponent> _cityResources;

        public override int Priority => SystemPriorities.RuntimeTick.BuildDistrictAction;

        public BuildDistrictActionSystem(World world)
            : base(world.GetEntities().With<DistrictBuildConfirmedEvent>().AsSet())
        {
            _world = world;

            _mayorActor = world.GetEntities()
                .With<MayorIdComponent>().With<MayorTag>().With<MayorAPComponent>().With<ActorTypeComponent>().AsSet();
            _cityActor = world.GetEntities()
                .With<CityIdComponent>().With<CityTag>().With<ActorTypeComponent>().AsSet();
            _mayorResources = world.GetEntities()
                .With<MayorIdComponent>().With<MayorTag>().With<MayorResourceTag>().AsMultiMap<MayorIdComponent>();
            _cityResources = world.GetEntities()
                .With<CityIdComponent>().With<CityTag>().With<CityResourceTag>().AsMultiMap<CityIdComponent>();

            // Seed the shared action-id counter once; ids start at 1 (0 = unset).
            if (!world.Has<ActionIdAllocatorComponent>())
                world.Set(new ActionIdAllocatorComponent { Next = 1 });
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            var confirmed = pulse.Get<DistrictBuildConfirmedEvent>();
            var cost = ResolveCost(confirmed.Type);

            // Charge the price BEFORE committing the entity: a shortfall throws, so a half-built state can never exist.
            SpendCost(confirmed.Payer, cost);

            var entity = _world.CreateEntity();
            entity.Set(new HexIdComponent { Coords = confirmed.Coords });
            entity.Set(new DistrictTypeComponent { Value = confirmed.Type });
            entity.Set(new ActionIdComponent { Value = AllocateId() });
            entity.Set(new BuildDistrictTurnsComponent { TurnsLeft = cost.TurnsToBuild, TurnsToBuild = cost.TurnsToBuild });
            entity.Set(new ActorTypeComponent { Type = confirmed.Payer });
            entity.Set(new BuildDistrictInProgressTag());

            if (cost.TurnsToBuild == 0)
            {
                var eventEntity = _world.CreateEntity();
                eventEntity.Set(new BuildDistrictCompleteEvent());
                eventEntity.Set(new EventTag());
            }
        }

        // All-or-nothing spend: resources from the payer's stockpile + AP from the Mayor's pool. Affordability is
        // verified across the WHOLE price before any deduction — a shortfall is a broken invariant (the confirm is
        // UI-gated), so fail loud rather than half-charge.
        private void SpendCost(ActorType payer, DistrictBuildCostConfig cost)
        {
            var mayor = ResolveMayor();
            var mayorAp = mayor.Get<MayorAPComponent>().Value;
            var stacks = ResolvePayerStacks(payer, mayor);

            // Flatten the authored price list to a stack-allocated span (zero managed allocation in the system).
            var prices = cost.DistrictPrices;
            var count = prices?.Count ?? 0;
            Span<ResourceAmount> amounts = stackalloc ResourceAmount[count];
            for (var i = 0; i < count; i++)
                amounts[i] = new ResourceAmount { Type = prices[i].Type, Amount = prices[i].Amount };

            if (mayorAp < cost.ApPrice || !ResourceLedger.CanAfford(stacks, amounts))
                throw new InvalidOperationException(
                    $"BuildDistrictActionSystem: {payer} cannot afford district '{cost.DistrictType}' " +
                    $"(AP {mayorAp}/{cost.ApPrice}). Confirm should be UI-gated by DistrictBuildPriceUISubSystem.");

            ResourceLedger.Deduct(stacks, amounts);
            mayor.Set(new MayorAPComponent { Value = mayorAp - cost.ApPrice });
        }

        // The single Mayor actor that holds the AP pool. Missing it at confirm is a broken world (the overlay only
        // opens in Gameplay, where a Mayor exists) — fail loud.
        private Entity ResolveMayor()
        {
            if (_mayorActor.Count == 0)
                throw new InvalidOperationException("BuildDistrictActionSystem: no Mayor actor to charge AP.");

            return _mayorActor.GetEntities()[0];
        }

        // The payer's resource stockpile (Mayor or City). An owner with no stacks yet resolves to empty — the
        // affordability guard then rejects any non-zero price. A payer that is neither is an authoring/UI error.
        private ReadOnlySpan<Entity> ResolvePayerStacks(ActorType payer, in Entity mayor)
        {
            if (payer == ActorType.Mayor)
                return _mayorResources.TryGetEntities(mayor.Get<MayorIdComponent>(), out var mayorStacks)
                    ? mayorStacks
                    : ReadOnlySpan<Entity>.Empty;

            if (payer == ActorType.City)
            {
                if (_cityActor.Count == 0)
                    throw new InvalidOperationException("BuildDistrictActionSystem: no City actor to charge resources.");

                return _cityResources.TryGetEntities(_cityActor.GetEntities()[0].Get<CityIdComponent>(), out var cityStacks)
                    ? cityStacks
                    : ReadOnlySpan<Entity>.Empty;
            }

            throw new InvalidOperationException($"BuildDistrictActionSystem: unsupported payer '{payer}'.");
        }

        // The district's cost config (prices + AP price + turns), by DistrictType. Fail loud: a confirmed build with
        // no cost config is a broken invariant (the UI only offers configured districts), not a benign default.
        private DistrictBuildCostConfig ResolveCost(DistrictType type)
        {
            if (!_world.Has<DistrictBuildCostsConfigComponent>())
                throw new InvalidOperationException(
                    "BuildDistrictActionSystem: DistrictBuildCostsConfigComponent world component is missing.");

            if (!DistrictConfigLookup.TryFind(
                    _world.Get<DistrictBuildCostsConfigComponent>().Value?.Districts, type, c => c.DistrictType, out var cost))
                throw new InvalidOperationException(
                    $"BuildDistrictActionSystem: no DistrictBuildCostConfig for district type '{type}'.");

            return cost;
        }

        // Hands out the next unique action id and advances the shared counter (write via Set).
        private int AllocateId()
        {
            var id = _world.Get<ActionIdAllocatorComponent>().Next;
            _world.Set(new ActionIdAllocatorComponent { Next = id + 1 });
            return id;
        }

        public override void Dispose()
        {
            _mayorActor.Dispose();
            _cityActor.Dispose();
            _mayorResources.Dispose();
            _cityResources.Dispose();
            base.Dispose();
        }
    }
}
