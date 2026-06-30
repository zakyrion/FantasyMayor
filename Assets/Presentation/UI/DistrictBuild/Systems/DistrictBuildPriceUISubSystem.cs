using System;
using DefaultEcs;
using Domains.Actions.Components;
using Domains.Actions.Configs;
using Domains.Actors.City.Components;
using Domains.Actors.Components;
using Domains.Actors.Data;
using Domains.Actors.Mayor.Components;
using Domains.Economy.District.Data;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Data;
using Domains.Economy.Resource.Tags;
using JetBrains.Annotations;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Data;
using Presentation.UI.DistrictBuild.Views;
using UnityEngine;

namespace Presentation.UI.DistrictBuild.Systems
{
    // БУДІВНИЦТВО populator: reconciles the price section to the current DistrictBuildSelectionComponent — the
    // selected district's AP cost (vs the Mayor's pool; AP is always Mayor-paid) + one cost row per resource
    // price (vs the current payer's stockpile, short-marked). Owns the payer toggle state LOCALLY (not in ECS):
    // it listens to the view's PayerChanged and re-renders the cost column. Reads ECS directly (no read-model).
    [UsedImplicitly]
    public sealed class DistrictBuildPriceUISubSystem : DistrictBuildUISubSystem
    {
        private const int ExecutionPriority = 300;

        private readonly EntityMultiMap<ActorTypeComponent> _actors;
        private readonly EntityMultiMap<MayorIdComponent> _mayorResources;
        private readonly EntityMultiMap<CityIdComponent> _cityResources;

        private Payer _payer = Payer.Mayor;
        private bool _hooked;

        public override int Priority => ExecutionPriority;

        public DistrictBuildPriceUISubSystem(World world) : base(world)
        {
            _actors = world.GetEntities().With<ActorTypeComponent>().AsMultiMap<ActorTypeComponent>();
            _mayorResources = world.GetEntities()
                .With<MayorIdComponent>().With<ResourceTag>().AsMultiMap<MayorIdComponent>();
            _cityResources = world.GetEntities()
                .With<CityIdComponent>().With<ResourceTag>().AsMultiMap<CityIdComponent>();
        }

        public override void Populate(GameObject root)
        {
            var view = World.Get<DistrictBuildPriceUIViewComponent>().View;

            // The view outlives the subsystem; subscribe once to the local payer toggle.
            if (!_hooked)
            {
                view.PayerChanged += OnPayerChanged;
                _hooked = true;
            }

            Render(view);
        }

        private void OnPayerChanged(Payer payer)
        {
            if (payer == _payer)
                return;

            _payer = payer;
            Render(World.Get<DistrictBuildPriceUIViewComponent>().View);
        }

        private void Render(DistrictBuildPriceUIView view)
        {
            var selected = World.Get<DistrictBuildSelectionComponent>().Selected;
            if (!TryGetCost(selected, out var cost))
            {
                view.SetAp(0, 0);
                view.ClearCosts();
                view.SetPayer(_payer);
                return;
            }

            view.SetAp(cost.ApPrice, AmountOf(Payer.Mayor, ResourceType.ActionPoint));

            view.ClearCosts();
            var prices = cost.DistrictPrices;
            if (prices != null)
                foreach (var price in prices)
                    view.AddCost(
                        DistrictBuildLabels.ResourceIcon(price.Type),
                        DistrictBuildLabels.ResourceLabel(price.Type),
                        price.Amount,
                        AmountOf(_payer, price.Type));

            view.SetPayer(_payer);
        }

        private bool TryGetCost(DistrictType type, out ActionsDistrictBuildConfig cost)
        {
            cost = null;
            if (type == DistrictType.Unknown || !World.Has<ActionsDistrictsBuildConfigComponent>())
                return false;

            var entries = World.Get<ActionsDistrictsBuildConfigComponent>().Value?.Districts;
            if (entries == null)
                return false;

            for (var i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].DistrictType == type)
                {
                    cost = entries[i];
                    return true;
                }

            return false;
        }

        private int AmountOf(Payer payer, ResourceType type)
        {
            if (payer == Payer.Mayor)
            {
                if (_actors.TryGetEntities(new ActorTypeComponent { Type = ActorType.Mayor }, out var mayors)
                    && mayors.Length > 0
                    && _mayorResources.TryGetEntities(mayors[0].Get<MayorIdComponent>(), out var stacks))
                    return AmountIn(stacks, type);

                return 0;
            }

            if (_actors.TryGetEntities(new ActorTypeComponent { Type = ActorType.City }, out var cities)
                && cities.Length > 0
                && _cityResources.TryGetEntities(cities[0].Get<CityIdComponent>(), out var cityStacks))
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

            _actors.Dispose();
            _mayorResources.Dispose();
            _cityResources.Dispose();
            base.Dispose();
        }
    }
}
