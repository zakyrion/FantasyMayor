using System;
using EcsExtensions;
using Friflo.Engine.ECS;
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
using Domains.Economy.District.Events;
using Domains.Economy.District.Helpers;
using Domains.Economy.District.Tags;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Economy.DistrictBuildCost.Configs;
using Domains.Economy.Resource.Data;
using Domains.Economy.Resource.Helpers;
using Domains.Kernel.Data;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Modules.AxialSystem;

namespace Domains.Actions.BuildDistrictAction.Systems
{
    /// <summary>
    ///     Reactive: on the <see cref="BuildDistrictCancelEvent" /> pulse (raised by
    ///     <c>HexInfoPanelDistrictSystem.OnCancelled</c>) resolves the District row at the pulse's hex, its
    ///     matching in-progress verb row (via <c>DistrictIdFKComponent</c>), REFUNDS, then disposes BOTH rows —
    ///     the unified District row has exactly 2 exits: Built (terminal) or disposed on cancel
    ///     (FLOW_DISTRICT_BUILD unification, 2026-07-17). Same turn as confirm
    ///     (<c>BuildDistrictTurnsComponent.TurnsLeft == TurnsToBuild</c> — nothing has ticked yet) refunds AP +
    ///     resources in full; any later turn refunds only resources, floored proportional to turns-left
    ///     (<c>price * TurnsLeft / TurnsToBuild</c>). AP is always Mayor-paid (mirrors the R2 spend), so the AP
    ///     refund always targets the Mayor's pool regardless of the resource payer. A pulse with no matching
    ///     District row (or no matching verb row for it — a broken invariant) is a broken invariant — the cancel
    ///     control is UI-gated (only shown while a build is in progress on the selected hex) — so it throws, not
    ///     skips. See <c>Flows/FLOW_DISTRICT_BUILD.md</c> R5 and <c>Patterns/PATTERN_REACTIVE_SYSTEM.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictActionCancelSystem : UpdatedSystem
    {
        // District rows: HexIdFKComponent is shared by every hex-anchored entity kind (views, containers,
        // resources), so a bare ComponentIndex over it is ambiguous across kinds — scope to the archetype.
        private readonly ArchetypeQuery _districts;

        // In-progress verb rows indexed by their FK into the District PK space.
        private readonly ComponentIndex<DistrictIdFKComponent, int> _inProgressByDistrictId;

        // Actor rows (Table Rule), same resolution as BuildDistrictActionSystem's spend side.
        private readonly ArchetypeQuery _mayorActor;
        private readonly ArchetypeQuery _cityActor;
        private readonly ComponentIndex<MayorIdFKComponent, int> _mayorResources;
        private readonly ComponentIndex<CityIdFKComponent, int> _cityResources;

        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.RuntimeTick.BuildDistrictActionCancel;

        public BuildDistrictActionCancelSystem(EntityStore world)
            : base(world.Query<BuildDistrictCancelEvent>())
        {
            _world = world;

            _districts = world.Query<HexIdFKComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<DistrictTag>());
            _inProgressByDistrictId = world.ComponentIndex<DistrictIdFKComponent, int>();

            _mayorActor = world.Query<MayorIdComponent, MayorAPComponent, ActorTypeComponent>()
                .AllTags(Friflo.Engine.ECS.Tags.Get<MayorTag>());
            _cityActor = world.Query<CityIdComponent, ActorTypeComponent>()
                .AllTags(Friflo.Engine.ECS.Tags.Get<CityTag>());
            _mayorResources = world.ComponentIndex<MayorIdFKComponent, int>();
            _cityResources = world.ComponentIndex<CityIdFKComponent, int>();
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            var coords = pulse.GetComponent<BuildDistrictCancelEvent>().Coords;

            if (!TryGetDistrict(coords, out var district))
                throw new InvalidOperationException(
                    $"BuildDistrictActionCancelSystem: no District row at {coords} to cancel.");

            var districtId = district.GetComponent<DistrictIdComponent>().Value;
            var type = district.GetComponent<DistrictTypeComponent>().Value;

            if (!_inProgressByDistrictId[districtId].TryGetFirst(out var verb))
                throw new InvalidOperationException(
                    $"BuildDistrictActionCancelSystem: no in-progress verb row for district {districtId} to cancel.");

            var turns = verb.GetComponent<BuildDistrictTurnsComponent>();
            var payer = verb.GetComponent<ActorTypeComponent>().Type;

            Refund(payer, ResolveCost(type), turns);

            district.DeleteEntity();
            verb.DeleteEntity();

            _world.CreateEvent(new DistrictTableChangedEvent { Change = DistrictTableChange.Removed });
        }

        // Same-turn (TurnsLeft == TurnsToBuild, nothing ticked since confirm): AP + resources in full. Any later
        // turn: resources only, floored proportional to turns-left; AP is never refunded past the confirm turn.
        private void Refund(ActorType payer, DistrictBuildCostConfig cost, BuildDistrictTurnsComponent turns)
        {
            var sameTurn = turns.TurnsLeft == turns.TurnsToBuild;
            var mayor = ResolveMayor();
            var stacks = ResolvePayerStacks(payer, mayor);

            // Flatten the authored price list to a stack-allocated span (zero managed allocation in the system).
            var prices = cost.DistrictPrices;
            var count = prices?.Count ?? 0;
            Span<ResourceAmount> amounts = stackalloc ResourceAmount[count];
            for (var i = 0; i < count; i++)
            {
                var refund = sameTurn ? prices[i].Amount : prices[i].Amount * turns.TurnsLeft / turns.TurnsToBuild;
                amounts[i] = new ResourceAmount { Type = prices[i].Type, Amount = refund };
            }

            ResourceLedger.Credit(stacks, amounts);

            if (sameTurn)
            {
                var mayorAp = mayor.GetComponent<MayorAPComponent>().Value;
                mayor.AddComponent(new MayorAPComponent { Value = mayorAp + cost.ApPrice });
            }
        }

        private bool TryGetDistrict(HexCoord coords, out Entity district)
        {
            foreach (var candidate in _districts.Entities)
            {
                if (!candidate.GetComponent<HexIdFKComponent>().Coords.Equals(coords))
                    continue;

                district = candidate;
                return true;
            }

            district = default;
            return false;
        }

        // The single Mayor actor that holds the AP pool. Missing it here is the same broken-world case
        // BuildDistrictActionSystem guards against at confirm.
        private Entity ResolveMayor()
        {
            if (!_mayorActor.TryGetFirst(out var mayor))
                throw new InvalidOperationException("BuildDistrictActionCancelSystem: no Mayor actor to refund AP.");

            return mayor;
        }

        // The payer's resource stockpile (Mayor or City) — same resolution as BuildDistrictActionSystem's spend side.
        private Entities ResolvePayerStacks(ActorType payer, in Entity mayor)
        {
            if (payer == ActorType.Mayor)
                return _mayorResources[mayor.GetComponent<MayorIdComponent>().Value];

            if (payer == ActorType.City)
            {
                if (!_cityActor.TryGetFirst(out var cityActor))
                    throw new InvalidOperationException("BuildDistrictActionCancelSystem: no City actor to refund resources.");

                return _cityResources[cityActor.GetComponent<CityIdComponent>().Value];
            }

            throw new InvalidOperationException($"BuildDistrictActionCancelSystem: unsupported payer '{payer}'.");
        }

        // The district's cost config, by DistrictType — same shared lookup BuildDistrictActionSystem uses.
        private DistrictBuildCostConfig ResolveCost(DistrictType type)
        {
            if (!_world.HasWorldComponent<DistrictBuildCostsConfigComponent>())
                throw new InvalidOperationException(
                    "BuildDistrictActionCancelSystem: DistrictBuildCostsConfigComponent world component is missing.");

            if (!DistrictConfigLookup.TryFind(
                    _world.GetWorldComponent<DistrictBuildCostsConfigComponent>().Value?.Districts, type, c => c.DistrictType, out var cost))
                throw new InvalidOperationException(
                    $"BuildDistrictActionCancelSystem: no DistrictBuildCostConfig for district type '{type}'.");

            return cost;
        }
    }
}
