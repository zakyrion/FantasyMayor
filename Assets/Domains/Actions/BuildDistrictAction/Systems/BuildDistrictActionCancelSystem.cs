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
    ///     Reactive: on the <see cref="BuildDistrictCancelEvent" /> pulse (raised by
    ///     <c>HexInfoPanelDistrictSystem.OnCancelled</c>) finds the in-progress build at the pulse's hex and
    ///     REFUNDS then disposes it. Same turn as confirm (<c>BuildDistrictTurnsComponent.TurnsLeft ==
    ///     TurnsToBuild</c> — nothing has ticked yet) refunds AP + resources in full; any later turn refunds only
    ///     resources, floored proportional to turns-left (<c>price * TurnsLeft / TurnsToBuild</c>). AP is always
    ///     Mayor-paid (mirrors the R2 spend), so the AP refund always targets the Mayor's pool regardless of the
    ///     resource payer. A pulse with no matching in-progress hex is a broken invariant — the cancel control is
    ///     UI-gated (only shown while a build is in progress on the selected hex) — so it throws, not skips. See
    ///     <c>Flows/FLOW_DISTRICT_BUILD.md</c> R5 and <c>Patterns/PATTERN_REACTIVE_SYSTEM.md</c>.
    /// </summary>
    [UsedImplicitly]
    public sealed class BuildDistrictActionCancelSystem : UpdatedSystem
    {
        // In-progress builds indexed by hex — the pulse's identifying payload resolves straight to the entity.
        private readonly EntityMultiMap<HexIdFKComponent> _inProgressByHex;

        // Actor rows (Table Rule), same resolution as BuildDistrictActionSystem's spend side.
        private readonly EntitySet _mayorActor;
        private readonly EntitySet _cityActor;
        private readonly EntityMultiMap<MayorIdFKComponent> _mayorResources;
        private readonly EntityMultiMap<CityIdFKComponent> _cityResources;

        private readonly World _world;

        public override int Priority => SystemPriorities.RuntimeTick.BuildDistrictActionCancel;

        public BuildDistrictActionCancelSystem(World world)
            : base(world.GetEntities().With<BuildDistrictCancelEvent>().AsSet())
        {
            _world = world;

            _inProgressByHex = world.GetEntities()
                .With<BuildDistrictInProgressTag>()
                .With<HexIdFKComponent>()
                .With<DistrictTypeFKComponent>()
                .With<BuildDistrictTurnsComponent>()
                .With<ActorTypeComponent>()
                .AsMultiMap<HexIdFKComponent>();

            _mayorActor = world.GetEntities()
                .With<MayorIdComponent>().With<MayorTag>().With<MayorAPComponent>().With<ActorTypeComponent>().AsSet();
            _cityActor = world.GetEntities()
                .With<CityIdComponent>().With<CityTag>().With<ActorTypeComponent>().AsSet();
            _mayorResources = world.GetEntities()
                .With<MayorIdFKComponent>().With<MayorResourceTag>().AsMultiMap<MayorIdFKComponent>();
            _cityResources = world.GetEntities()
                .With<CityIdFKComponent>().With<CityResourceTag>().AsMultiMap<CityIdFKComponent>();
        }

        protected override void Update(GameState state, in Entity pulse)
        {
            var coords = pulse.Get<BuildDistrictCancelEvent>().Coords;

            if (!_inProgressByHex.TryGetEntities(new HexIdFKComponent { Coords = coords }, out var matches) || matches.Length == 0)
                throw new InvalidOperationException(
                    $"BuildDistrictActionCancelSystem: no in-progress build at {coords} to cancel.");

            var entity = matches[0];
            var turns = entity.Get<BuildDistrictTurnsComponent>();
            var type = entity.Get<DistrictTypeFKComponent>().Value;
            var payer = entity.Get<ActorTypeComponent>().Type;

            Refund(payer, ResolveCost(type), turns);

            entity.Dispose();
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
                var mayorAp = mayor.Get<MayorAPComponent>().Value;
                mayor.Set(new MayorAPComponent { Value = mayorAp + cost.ApPrice });
            }
        }

        // The single Mayor actor that holds the AP pool. Missing it here is the same broken-world case
        // BuildDistrictActionSystem guards against at confirm.
        private Entity ResolveMayor()
        {
            if (_mayorActor.Count == 0)
                throw new InvalidOperationException("BuildDistrictActionCancelSystem: no Mayor actor to refund AP.");

            return _mayorActor.GetEntities()[0];
        }

        // The payer's resource stockpile (Mayor or City) — same resolution as BuildDistrictActionSystem's spend side.
        private ReadOnlySpan<Entity> ResolvePayerStacks(ActorType payer, in Entity mayor)
        {
            if (payer == ActorType.Mayor)
                return _mayorResources.TryGetEntities(
                    new MayorIdFKComponent { Value = mayor.Get<MayorIdComponent>().Value }, out var mayorStacks)
                    ? mayorStacks
                    : ReadOnlySpan<Entity>.Empty;

            if (payer == ActorType.City)
            {
                if (_cityActor.Count == 0)
                    throw new InvalidOperationException("BuildDistrictActionCancelSystem: no City actor to refund resources.");

                var cityId = _cityActor.GetEntities()[0].Get<CityIdComponent>().Value;
                return _cityResources.TryGetEntities(new CityIdFKComponent { Value = cityId }, out var cityStacks)
                    ? cityStacks
                    : ReadOnlySpan<Entity>.Empty;
            }

            throw new InvalidOperationException($"BuildDistrictActionCancelSystem: unsupported payer '{payer}'.");
        }

        // The district's cost config, by DistrictType — same shared lookup BuildDistrictActionSystem uses.
        private DistrictBuildCostConfig ResolveCost(DistrictType type)
        {
            if (!_world.Has<DistrictBuildCostsConfigComponent>())
                throw new InvalidOperationException(
                    "BuildDistrictActionCancelSystem: DistrictBuildCostsConfigComponent world component is missing.");

            if (!DistrictConfigLookup.TryFind(
                    _world.Get<DistrictBuildCostsConfigComponent>().Value?.Districts, type, c => c.DistrictType, out var cost))
                throw new InvalidOperationException(
                    $"BuildDistrictActionCancelSystem: no DistrictBuildCostConfig for district type '{type}'.");

            return cost;
        }

        public override void Dispose()
        {
            _inProgressByHex.Dispose();
            _mayorActor.Dispose();
            _cityActor.Dispose();
            _mayorResources.Dispose();
            _cityResources.Dispose();
            base.Dispose();
        }
    }
}
