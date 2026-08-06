using System;
using Domains.Actors.Archetypes;
using Domains.Actors.City.Components;
using Domains.Actors.Mayor.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Helpers;
using Domains.Economy.DistrictBuild.Components;
using Domains.Economy.DistrictBuild.Configs;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Economy.DistrictBuildCost.Configs;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Data;
using Domains.Kernel.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.UI.Archetypes;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Tags;
using Presentation.UI.DistrictBuild.Views;
using UnityEngine;

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
        private readonly EntityStorages _storages;
        private readonly Archetype _selectionSet;

        // Actor rows (Table Rule): id PK + ActorTypeComponent discriminator — never a bare key.
        private readonly Archetype _mayorActor;
        private readonly Archetype _cityActor;
        private readonly ComponentIndex<MayorIdFKComponent, int> _mayorResources;
        private readonly ComponentIndex<CityIdFKComponent, int> _cityResources;

        // The chrome view lives on an ENTITY (UITag), not as a singleton component — resolve it the way the orchestrator does.
        private readonly Archetype _chrome;

        private bool _hooked;

        public override int Priority => SystemPriorities.SubSystems.DistrictBuildUi.Price;

        public DistrictBuildPriceUISubSystem(EntityStorages storages) : base(storages.World)
        {
            _storages = storages;
            _selectionSet = PresentationUIArchetypes.DistrictBuildSelection(storages.World);
            _mayorActor = ActorsArchetypes.Mayor(storages.World);
            _cityActor = ActorsArchetypes.City(storages.World);
            _mayorResources = storages.World.ComponentIndex<MayorIdFKComponent, int>();
            _cityResources = storages.World.ComponentIndex<CityIdFKComponent, int>();
            _chrome = PresentationUIArchetypes.DistrictBuildUI(storages.World);
        }

        public override void Populate(GameObject root)
        {
            var view = _storages.Singletons.Get<DistrictBuildPriceUIViewComponent>().View;

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
            Render(_storages.Singletons.Get<DistrictBuildPriceUIViewComponent>().View);
        }

        private void Render(DistrictBuildPriceUIView view)
        {
            if (!_selectionSet.TryGetFirst(out var selectionEntity))
                throw new InvalidOperationException(
                    $"DistrictBuildPriceUISubSystem: Populate called with no active {nameof(DistrictBuildSelectionTag)} entity.");

            var selected = selectionEntity.GetComponent<DistrictBuildSelectionComponent>().Selected;
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

            if (!_mayorActor.TryGetFirst(out var mayorEntity))
                throw new InvalidOperationException("DistrictBuildPriceUISubSystem: no Mayor actor to read AP from.");

            var ap = mayorEntity.GetComponent<MayorAPComponent>().Value;
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
        // singleton component). Affordability is known only here, so the price section is the single source of truth for the
        // gate — it stays in sync on both populate and payer switch because Render is the choke point for both.
        private void PushConfirmGate(bool affordable)
        {
            if (!_chrome.TryGetFirst(out var chromeEntity))
                return;

            var chrome = chromeEntity.GetComponent<DistrictBuildUIViewComponent>().View;
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

            if (type == DistrictType.None || !_storages.Singletons.Has<DistrictBuildCostsConfigComponent>())
                return false;

            return DistrictConfigLookup.TryFind(
                _storages.Singletons.Get<DistrictBuildCostsConfigComponent>().Value?.Districts, type, c => c.DistrictType, out cost);
        }

        private bool TryGetDistrict(DistrictType type, out DistrictBuildConfig district)
        {
            district = null;
            if (type == DistrictType.Unknown)
                throw new InvalidOperationException(
                    $"{nameof(DistrictBuildSelectionComponent)}.{nameof(DistrictBuildSelectionComponent.Selected)} " +
                    $"is {DistrictType.Unknown} — the selection must be a real district or {nameof(DistrictType.None)}, " +
                    "never the error marker.");

            if (type == DistrictType.None || !_storages.Singletons.Has<DistrictBuildsConfigComponent>())
                return false;

            return DistrictConfigLookup.TryFind(
                _storages.Singletons.Get<DistrictBuildsConfigComponent>().Value?.Districts, type, d => d.DistrictType, out district);
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
                if (!_mayorActor.TryGetFirst(out var mayor))
                    return 0;

                return AmountIn(_mayorResources[mayor.GetComponent<MayorIdComponent>().Value], type);
            }

            if (!_cityActor.TryGetFirst(out var city))
                return 0;

            return AmountIn(_cityResources[city.GetComponent<CityIdComponent>().Value], type);
        }

        private static int AmountIn(Entities stacks, ResourceType type)
        {
            foreach (var stack in stacks)
            {
                var resource = stack.GetComponent<ResourceComponent>();
                if (resource.Type == type)
                    return resource.Amount;
            }

            return 0;
        }

        public override void Dispose()
        {
            if (_hooked && _storages.Singletons.Has<DistrictBuildPriceUIViewComponent>())
            {
                var view = _storages.Singletons.Get<DistrictBuildPriceUIViewComponent>().View;
                if (view != null)
                    view.PayerChanged -= OnPayerChanged;
            }

            base.Dispose();
        }
    }
}
