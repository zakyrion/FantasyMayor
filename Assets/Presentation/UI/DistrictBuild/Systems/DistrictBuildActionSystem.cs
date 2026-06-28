using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actors.City.Components;
using Domains.Actors.Components;
using Domains.Actors.Data;
using Domains.Actors.Mayor.Components;
using Domains.Economy.District.Components;
using Domains.Economy.Resource.Components;
using Domains.Economy.Resource.Tags;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Tags;
using Domains.Map.HexResources.Components;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Terrain.Components;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Events;
using Presentation.UI.DistrictBuild.Views;
using UnityEngine;

namespace Presentation.UI.DistrictBuild.Systems
{
    /// <summary>
    ///     Drives the district-build overlay's visibility + content. Anchored on the DistrictBuildActionViewComponent
    ///     singleton so it ticks once per frame (like EndTurnSystem / ResourceBarSystem): it coalesces the two
    ///     one-frame pulses for this one window — <see cref="DistrictBuildRequestedEvent" /> (open) and
    ///     <see cref="DistrictBuildClosedEvent" /> (hide). On open it pushes the catalogue reference + each payer's
    ///     stockpile amounts straight into the view (one call per stack, no intermediate collection — the
    ///     zero-allocation read path of ResourceBarSystem). The window is modal, so the selection cannot change
    ///     while it is open; it fills once on open.
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictBuildActionSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 565;

        private readonly World _world;
        private readonly EntitySet _requestedSet;
        private readonly EntitySet _closedSet;
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _hexSet;

        // FK 1:N indexes (Table Rule), as in ResourceBarSystem: owner id is a PK on the actor AND a FK on the stack.
        private readonly EntityMultiMap<ActorTypeComponent> _actors;
        private readonly EntityMultiMap<CityIdComponent> _cityResources;
        private readonly EntityMultiMap<MayorIdComponent> _mayorResources;
        // Hex resources live on dedicated entities (HexIdComponent FK + HexResourcesComponent), 1:N per hex.
        private readonly EntityMultiMap<HexIdComponent> _hexResources;

        public override int Priority => ExecutionPriority;

        public DistrictBuildActionSystem(World world)
            : base(world.GetEntities().With<DistrictBuildActionViewComponent>().AsSet())
        {
            _world = world;
            _requestedSet = world.GetEntities().With<DistrictBuildRequestedEvent>().AsSet();
            _closedSet = world.GetEntities().With<DistrictBuildClosedEvent>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().AsSet();
            _hexSet = world.GetEntities().With<HexTag>().With<HexIdComponent>().AsSet();

            _actors = world.GetEntities().With<ActorTypeComponent>().AsMultiMap<ActorTypeComponent>();
            _cityResources = world.GetEntities()
                .With<CityIdComponent>().With<ResourceTag>().AsMultiMap<CityIdComponent>();
            _mayorResources = world.GetEntities()
                .With<MayorIdComponent>().With<ResourceTag>().AsMultiMap<MayorIdComponent>();
            _hexResources = world.GetEntities()
                .With<HexResourceComponent>().With<HexIdComponent>().AsMultiMap<HexIdComponent>();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.Get<DistrictBuildActionViewComponent>().View;
            if (view == null)
            {
                Debug.Log($"[skh] no DistrictBuildActionViewComponent");
                return;
            }

            if (_closedSet.Count > 0)
                view.Hide();

            if (_requestedSet.Count > 0)
                Open(view);
        }

        private void Open(DistrictBuildActionView view)
        {
            Debug.Log($"[skh] Open DistrictBuildActionViewComponent");
            // The build prompt only exists while a hex is selected; a stray request without one is a no-op.
            if (_selectedHexSet.Count == 0)
                return;

            if (!_world.Has<DistrictsBuildConfigComponent>())
                throw new InvalidOperationException(
                    "DistrictBuildActionSystem: DistrictsBuildConfigComponent is missing (config not loaded).");

            var coords = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;

            // A non-grid coordinate carries no hex — a valid empty selection; nothing to build on, so skip.
            if (!TryGetHexType(coords, out var hexType))
                return;

            // Reference, not a copy — the SO holds the catalogue; the loader keeps it alive.
            view.SetContext(_world.Get<DistrictsBuildConfigComponent>().Value, hexType);

            FillHexResources(view, coords);
            FillPayers(view);
            view.Show();
        }

        // Pushes each payer's AP + per-type stockpile amounts straight into the view (no collection crosses the
        // boundary — the view resets its own pools in SetContext, then records each amount by type).
        private void FillPayers(DistrictBuildActionView view)
        {
            if (_actors.TryGetEntities(new ActorTypeComponent { Type = ActorType.Mayor }, out var mayors)
                && mayors.Length > 0)
            {
                view.SetMayorAp(mayors[0].Get<MayorAPComponent>().Value);

                if (_mayorResources.TryGetEntities(mayors[0].Get<MayorIdComponent>(), out var mayorStacks))
                {
                    foreach (var stack in mayorStacks)
                    {
                        var resource = stack.Get<ResourceComponent>();
                        view.SetMayorResource(resource.Type, resource.Amount);
                    }
                }
            }

            if (_actors.TryGetEntities(new ActorTypeComponent { Type = ActorType.City }, out var cities)
                && cities.Length > 0
                && _cityResources.TryGetEntities(cities[0].Get<CityIdComponent>(), out var cityStacks))
            {
                foreach (var stack in cityStacks)
                {
                    var resource = stack.Get<ResourceComponent>();
                    view.SetCityResource(resource.Type, resource.Amount);
                }
            }
        }

        // Pushes the selected hex's resource types into the view (one per stack, no collection crosses the
        // boundary — same push pattern as FillPayers). A hex with no resource entities pushes nothing, so the
        // view's reset buffer reads as empty — which is exactly what the empty-hex build gate needs.
        private void FillHexResources(DistrictBuildActionView view, HexCoord coords)
        {
            if (!_hexResources.TryGetEntities(new HexIdComponent { Coords = coords }, out var resources))
                return;

            foreach (var resourceEntity in resources)
                view.AddHexResource(resourceEntity.Get<HexResourceComponent>().Type);
        }

        private bool TryGetHexType(HexCoord coords, out HexType type)
        {
            type = default;

            foreach (var hexEntity in _hexSet.GetEntities())
            {
                if (hexEntity.Get<HexIdComponent>().Coords != coords)
                    continue;

                type = hexEntity.Get<HexTypeComponent>().Type;
                return true;
            }

            return false;
        }

        public override void Dispose()
        {
            _requestedSet.Dispose();
            _closedSet.Dispose();
            _selectedHexSet.Dispose();
            _hexSet.Dispose();
            _actors.Dispose();
            _cityResources.Dispose();
            _mayorResources.Dispose();
            _hexResources.Dispose();
            base.Dispose();
        }
    }
}
