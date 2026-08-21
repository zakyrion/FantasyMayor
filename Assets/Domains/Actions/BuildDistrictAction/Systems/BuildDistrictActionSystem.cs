using System;
using Domains.Actions.Archetypes;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Actions.Components;
using Domains.Actors.Archetypes;
using Domains.Actors.City.Components;
using Domains.Actors.Components;
using Domains.Actors.Mayor.Components;
using Domains.Economy.Archetypes;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Economy.District.Helpers;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Economy.DistrictBuildCost.Configs;
using Domains.Economy.Resource.Data;
using Domains.Economy.Resource.Helpers;
using Domains.Kernel.Data;
using Domains.Map.Hex.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Reactive: on the <see cref="DistrictBuildConfirmedEvent" /> pulse SPENDS the district's price then creates
    ///     the UNIFIED district row: the Economy <c>District</c> fact (<c>DistrictTag</c> + allocated
    ///     <c>DistrictIdComponent</c> PK, seeded here — <c>DistrictIdAllocatorComponent</c> — + <c>HexIdFKComponent</c>
    ///     + <c>DistrictTypeComponent</c>) staged <c>DistrictBuildState.Planned</c>, and the pure-verb in-progress
    ///     row (<c>BuildDistrictInProgressTag</c> + <c>DistrictIdFKComponent</c> back into that row, a unique
    ///     <c>ActionIdComponent</c>, the turn countdown, the payer) — the verb row carries NO district attribute
    ///     (FLOW_DISTRICT_BUILD unification, 2026-07-17). Spend (R2) runs first and all-or-nothing: the payer's
    ///     resource stockpile is charged the <c>DistrictBuildCostConfig.DistrictPrices</c> (via
    ///     <c>ResourceLedger</c>) and the Mayor's <c>MayorAPComponent</c> pool is charged the <c>ApPrice</c> (AP is
    ///     always Mayor-paid). Affordability is checked across the WHOLE price before any deduction, and a
    ///     shortfall throws — the confirm is UI-gated (<c>DistrictBuildPriceUISubSystem</c> disables «Збудувати»
    ///     when unaffordable), so a shortfall here is a broken invariant, not a normal path. There is no draft: the
    ///     build is committed directly on confirm. Hex, district, and payer come from the pulse (the Actions
    ///     assembly can't read the Presentation selection). <c>BuildDistrictTurnTickSystem</c> counts the verb row
    ///     down each turn and <c>BuildDistrictCompletionSystem</c> flips the District row to Built at zero. See
    ///     <c>Patterns/PATTERN_REACTIVE_SYSTEM.md</c> and <c>Flows/FLOW_DISTRICT_BUILD.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictActionSystem : UpdatedSystem
    {
        private readonly EntityStorages _storages;

        // Actor rows (Table Rule): id PK + tag + ActorTypeComponent discriminator; the Mayor also carries the AP pool.
        private readonly Archetype _mayorActor;
        private readonly Archetype _cityActor;

        // Resource stacks grouped by owner id — the payer's stockpile handed to ResourceLedger for the spend.
        private readonly ComponentIndex<MayorIdFKComponent, int> _mayorResources;
        private readonly ComponentIndex<CityIdFKComponent, int> _cityResources;

        private readonly Archetype _districtArchetype;
        private readonly Archetype _buildInProgressArchetype;

        public override int Priority => SystemPriorities.RuntimeTick.BuildDistrictAction;

        public BuildDistrictActionSystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<DistrictBuildConfirmedEvent>(storages.World))
        {
            _storages = storages;

            _mayorActor = ActorsArchetypes.Mayor(storages.World);
            _cityActor = ActorsArchetypes.City(storages.World);
            _mayorResources = storages.World.ComponentIndex<MayorIdFKComponent, int>();
            _cityResources = storages.World.ComponentIndex<CityIdFKComponent, int>();
            _districtArchetype = EconomyArchetypes.District(storages.World);
            _buildInProgressArchetype = ActionsArchetypes.BuildDistrictInProgress(storages.World);

            // Seed the shared action-id counter once; ids start at 1 (0 = unset).
            if (!storages.Singletons.Has<ActionIdAllocatorComponent>())
                storages.Singletons.Set(new ActionIdAllocatorComponent { Next = 1 });

            // Seed the district-id counter once; ids start at 1 (0 = unset). Moved here from
            // BuildDistrictCompletionSystem (FLOW_DISTRICT_BUILD unification): the PK is allocated at CONFIRM now.
            if (!storages.Singletons.Has<DistrictIdAllocatorComponent>())
                storages.Singletons.Set(new DistrictIdAllocatorComponent { Next = 1 });
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            var confirmed = pulse.GetComponent<DistrictBuildConfirmedEvent>();
            var cost = ResolveCost(confirmed.Type);

            // Charge the price BEFORE committing any row: a shortfall throws, so a half-built state can never exist.
            SpendCost(confirmed.Payer, cost);

            var districtId = AllocateDistrictId();

            var district = _districtArchetype.CreateEntity();
            district.AddComponent(new DistrictIdComponent { Value = districtId });
            district.AddComponent(new HexIdFKComponent { Coords = confirmed.Coords });
            district.AddComponent(new DistrictTypeComponent { Value = confirmed.Type });
            district.AddComponent(new DistrictBuildStateComponent { Value = DistrictBuildState.Planned });

            var entity = _buildInProgressArchetype.CreateEntity();
            entity.AddComponent(new DistrictIdFKComponent { Value = districtId });
            entity.AddComponent(new ActionIdComponent { Value = AllocateId() });
            entity.AddComponent(new BuildDistrictTurnsComponent { TurnsLeft = cost.TurnsToBuild, TurnsToBuild = cost.TurnsToBuild });
            entity.AddComponent(new ActorTypeComponent { Type = confirmed.Payer });

            _storages.World.CreateEvent(new DistrictTableChangedEvent { Change = DistrictTableChange.Planned });

            if (cost.TurnsToBuild == 0)
                _storages.World.CreateEvent(new BuildDistrictCompleteEvent());
        }

        // All-or-nothing spend: resources from the payer's stockpile + AP from the Mayor's pool. Affordability is
        // verified across the WHOLE price before any deduction — a shortfall is a broken invariant (the confirm is
        // UI-gated), so fail loud rather than half-charge.
        private void SpendCost(ActorType payer, DistrictBuildCostConfig cost)
        {
            var mayor = ResolveMayor();
            var mayorAp = mayor.GetComponent<MayorAPComponent>().Value;
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
            mayor.AddComponent(new MayorAPComponent { Value = mayorAp - cost.ApPrice });
        }

        // The single Mayor actor that holds the AP pool. Missing it at confirm is a broken world (the overlay only
        // opens in Gameplay, where a Mayor exists) — fail loud.
        private Entity ResolveMayor()
        {
            if (!_mayorActor.TryGetFirst(out var mayor))
                throw new InvalidOperationException("BuildDistrictActionSystem: no Mayor actor to charge AP.");

            return mayor;
        }

        // The payer's resource stockpile (Mayor or City). An owner with no stacks yet resolves to empty — the
        // affordability guard then rejects any non-zero price. A payer that is neither is an authoring/UI error.
        private Entities ResolvePayerStacks(ActorType payer, in Entity mayor)
        {
            if (payer == ActorType.Mayor)
                return _mayorResources[mayor.GetComponent<MayorIdComponent>().Value];

            if (payer == ActorType.City)
            {
                if (!_cityActor.TryGetFirst(out var cityActor))
                    throw new InvalidOperationException("BuildDistrictActionSystem: no City actor to charge resources.");

                return _cityResources[cityActor.GetComponent<CityIdComponent>().Value];
            }

            throw new InvalidOperationException($"BuildDistrictActionSystem: unsupported payer '{payer}'.");
        }

        // The district's cost config (prices + AP price + turns), by DistrictType. Fail loud: a confirmed build with
        // no cost config is a broken invariant (the UI only offers configured districts), not a benign default.
        private DistrictBuildCostConfig ResolveCost(DistrictType type)
        {
            if (!_storages.Singletons.Has<DistrictBuildCostsConfigComponent>())
                throw new InvalidOperationException(
                    "BuildDistrictActionSystem: DistrictBuildCostsConfigComponent singleton component is missing.");

            if (!DistrictConfigLookup.TryFind(
                    _storages.Singletons.Get<DistrictBuildCostsConfigComponent>().Value?.Districts, type, c => c.DistrictType, out var cost))
                throw new InvalidOperationException(
                    $"BuildDistrictActionSystem: no DistrictBuildCostConfig for district type '{type}'.");

            return cost;
        }

        // Hands out the next unique action id and advances the shared counter (write via AddComponent).
        private int AllocateId()
        {
            var id = _storages.Singletons.Get<ActionIdAllocatorComponent>().Next;
            _storages.Singletons.Set(new ActionIdAllocatorComponent { Next = id + 1 });
            return id;
        }

        // Hands out the next unique district id and advances the shared counter (write via AddComponent).
        private int AllocateDistrictId()
        {
            var id = _storages.Singletons.Get<DistrictIdAllocatorComponent>().Next;
            _storages.Singletons.Set(new DistrictIdAllocatorComponent { Next = id + 1 });
            return id;
        }
    }
}
