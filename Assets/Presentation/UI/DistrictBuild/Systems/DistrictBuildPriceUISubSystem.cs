using System;
using DefaultEcs;
using Domains.Actors.City.Components;
using Domains.Actors.Components;
using Domains.Actors.Mayor.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Helpers;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Data;
using JetBrains.Annotations;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Tags;
using Presentation.UI.DistrictBuild.Views;
using UnityEngine;
using Domains.Economy.DistrictBuildCost.Configs;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Economy.DistrictBuild.Configs;
using Domains.Economy.DistrictBuild.Components;
using Domains.Kernel.Data;
using DefaultECSExtensions;
using Domains.Actors.City.Tags;
using Domains.Actors.Mayor.Tags;
using Presentation.UI.Tags;

namespace Presentation.UI.DistrictBuild.Systems
{
    // БУДІВНИЦТВО populator: reconciles the price section to the current DistrictBuildSelectionComponent — the
    // selected district's AP cost (vs the Mayor's pool; AP is always Mayor-paid) + one payer segment per allowed
    // owner + one cost row per resource price (vs the selected payer's stockpile, short-marked). Payer selection
    // lives in the view (not ECS): this reads view.SelectedOwner and re-renders on the view's PayerChanged.
    // Reads ECS directly (no read-model).
    [UsedImplicitly]
    public sealed class DistrictBuildPriceUISubSystem : DistrictBuildUISubSystem
    {
        private readonly EntitySet _selectionSet;

        // Actor rows (Table Rule): id PK + ActorTypeComponent discriminator — never a bare key.
        private readonly EntitySet _mayorActor;
        private readonly EntitySet _cityActor;
        private readonly EntityMultiMap<MayorIdComponent> _mayorResources;
        private readonly EntityMultiMap<CityIdComponent> _cityResources;

        // The chrome view lives on an ENTITY (UITag), not as a world component — resolve it the way the orchestrator does.
        private readonly EntitySet _chrome;

        private bool _hooked;

        public override int Priority => SystemPriorities.SubSystems.DistrictBuildUi.Price;

        public DistrictBuildPriceUISubSystem(World world) : base(world)
        {
            _selectionSet = world.GetEntities().With<DistrictBuildSelectionTag>().AsSet();
            _mayorActor = world.GetEntities().With<MayorIdComponent>().With<MayorTag>().With<MayorAPComponent>().With<ActorTypeComponent>().AsSet();
            _cityActor = world.GetEntities().With<CityIdComponent>().With<CityTag>().With<ActorTypeComponent>().AsSet();
            _mayorResources = world.GetEntities()
                .With<MayorIdComponent>().With<MayorTag>().With<MayorResourceTag>().AsMultiMap<MayorIdComponent>();
            _cityResources = world.GetEntities()
                .With<CityIdComponent>().With<CityTag>().With<CityResourceTag>().AsMultiMap<CityIdComponent>();
            _chrome = world.GetEntities().With<DistrictBuildUIViewComponent>().With<UITag>().AsSet();
        }

        public override void Populate(GameObject root)
        {
            var view = World.Get<DistrictBuildPriceUIViewComponent>().View;

            // The view outlives the subsystem; subscribe once to the payer selection.
            if (!_hooked)
            {
                view.PayerChanged += OnPayerChanged;
                _hooked = true;
            }

            Render(view);
        }

        private void OnPayerChanged(ActorType owner)
        {
            Render(World.Get<DistrictBuildPriceUIViewComponent>().View);
        }

        private void Render(DistrictBuildPriceUIView view)
        {
            var selected = _selectionSet.GetEntities()[0].Get<DistrictBuildSelectionComponent>().Selected;
            if (!TryGetCost(selected, out var cost) || !TryGetDistrict(selected, out var district))
            {
                view.SetAp(0, 0);
                view.ClearCosts();
                view.ClearPayers();
                PushConfirmGate(false); // no real district selected → nothing to build
                return;
            }

            var allowed = district.AllowedOwners;
            var payer = view.SelectedOwner;
            if (payer == ActorType.Unknown || !allowed.HasFlag(payer))
                payer = DefaultOwner(selected, allowed);

            var ap = _mayorActor.GetEntities()[0].Get<MayorAPComponent>().Value;
            view.SetAp(cost.ApPrice, ap);

            view.ClearPayers();
            if (allowed.HasFlag(ActorType.Mayor))
                view.AddPayer(ActorType.Mayor, DistrictBuildLabels.OwnerIcon(ActorType.Mayor), DistrictBuildLabels.OwnerLabel(ActorType.Mayor));
            if (allowed.HasFlag(ActorType.City))
                view.AddPayer(ActorType.City, DistrictBuildLabels.OwnerIcon(ActorType.City), DistrictBuildLabels.OwnerLabel(ActorType.City));
            view.SetSelectedPayer(payer);

            // Affordability spans the WHOLE price (AP + every resource for the selected payer); it gates the chrome
            // confirm so the BuildDistrictActionSystem spend-guard throw never fires on a normal click.
            view.ClearCosts();
            var affordable = ap >= cost.ApPrice;
            var prices = cost.DistrictPrices;
            if (prices != null)
                foreach (var price in prices)
                {
                    var have = AmountOf(payer, price.Type);
                    view.AddCost(
                        DistrictBuildLabels.ResourceIcon(price.Type),
                        DistrictBuildLabels.ResourceLabel(price.Type),
                        price.Amount,
                        have);
                    if (have < price.Amount)
                        affordable = false;
                }

            PushConfirmGate(affordable);
        }

        // Drives the chrome confirm button (owned by DistrictBuildUIView, which lives on the UITag chrome entity — NOT a
        // world component). Affordability is known only here, so the price section is the single source of truth for the
        // gate — it stays in sync on both populate and payer switch because Render is the choke point for both.
        private void PushConfirmGate(bool affordable)
        {
            if (_chrome.Count == 0)
                return;

            var chrome = _chrome.GetEntities()[0].Get<DistrictBuildUIViewComponent>().View;
            if (chrome != null)
                chrome.SetConfirmEnabled(affordable);
        }

        private bool TryGetCost(DistrictType type, out DistrictBuildCostConfig cost)
        {
            cost = null;
            if (type == DistrictType.Unknown)
                throw new InvalidOperationException(
                    $"{nameof(DistrictBuildSelectionComponent)}.{nameof(DistrictBuildSelectionComponent.Selected)} " +
                    $"is {DistrictType.Unknown} — the selection must be a real district or {nameof(DistrictType.None)}, " +
                    "never the error marker.");

            if (type == DistrictType.None || !World.Has<DistrictBuildCostsConfigComponent>())
                return false;

            return DistrictConfigLookup.TryFind(
                World.Get<DistrictBuildCostsConfigComponent>().Value?.Districts, type, c => c.DistrictType, out cost);
        }

        private bool TryGetDistrict(DistrictType type, out DistrictBuildConfig district)
        {
            district = null;
            if (type == DistrictType.Unknown)
                throw new InvalidOperationException(
                    $"{nameof(DistrictBuildSelectionComponent)}.{nameof(DistrictBuildSelectionComponent.Selected)} " +
                    $"is {DistrictType.Unknown} — the selection must be a real district or {nameof(DistrictType.None)}, " +
                    "never the error marker.");

            if (type == DistrictType.None || !World.Has<DistrictBuildsConfigComponent>())
                return false;

            return DistrictConfigLookup.TryFind(
                World.Get<DistrictBuildsConfigComponent>().Value?.Districts, type, d => d.DistrictType, out district);
        }

        // Default payer when the view has no valid selection yet: first allowed owner in canonical order
        // (Mayor before City). A district that allows no owner is an authoring error — fail loud.
        private static ActorType DefaultOwner(DistrictType type, ActorType allowed)
        {
            if (allowed.HasFlag(ActorType.Mayor))
                return ActorType.Mayor;
            if (allowed.HasFlag(ActorType.City))
                return ActorType.City;

            throw new InvalidOperationException(
                $"DistrictBuildConfig ({type}): AllowedOwners is {allowed} — a district must allow at least one owner.");
        }

        private int AmountOf(ActorType payer, ResourceType type)
        {
            if (payer == ActorType.Mayor)
            {
                if (_mayorActor.Count > 0
                    && _mayorResources.TryGetEntities(_mayorActor.GetEntities()[0].Get<MayorIdComponent>(), out var stacks))
                    return AmountIn(stacks, type);

                return 0;
            }

            if (_cityActor.Count > 0
                && _cityResources.TryGetEntities(_cityActor.GetEntities()[0].Get<CityIdComponent>(), out var cityStacks))
                return AmountIn(cityStacks, type);

            return 0;
        }

        private static int AmountIn(ReadOnlySpan<Entity> stacks, ResourceType type)
        {
            foreach (var stack in stacks)
            {
                var resource = stack.Get<ResourceComponent>();
                if (resource.Type == type)
                    return resource.Amount;
            }

            return 0;
        }

        public override void Dispose()
        {
            if (_hooked && World.Has<DistrictBuildPriceUIViewComponent>())
            {
                var view = World.Get<DistrictBuildPriceUIViewComponent>().View;
                if (view != null)
                    view.PayerChanged -= OnPayerChanged;
            }

            _selectionSet.Dispose();
            _mayorActor.Dispose();
            _cityActor.Dispose();
            _mayorResources.Dispose();
            _cityResources.Dispose();
            _chrome.Dispose();
            base.Dispose();
        }
    }
}
