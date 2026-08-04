using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using EcsExtensions;
using Friflo.Engine.ECS;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Data;
using Domains.Kernel.Data;
using Domains.Map.Hex.Components;
using Flows.DistrictBuild.Events;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Terrain.Components;
using Presentation.UI.Archetypes;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Tags;
using Presentation.UI.DistrictBuild.Views;
using Presentation.Terrain.Tags;
using Presentation.UI.Tags;

namespace Presentation.UI.DistrictBuild.Systems
{
    /// <summary>
    ///     Drives the district-build overlay: visibility + section dispatch. Anchored on the
    ///     DistrictBuildUIViewComponent singleton (ticks once per frame). Opens on the external
    ///     <see cref="DistrictBuildUIRequestedEvent" /> pulse (raised by HexInfoPanel). Chrome is C#-event driven: it
    ///     subscribes to the view's Confirmed (read the ECS selection → raise the cross-domain
    ///     <see cref="DistrictBuildConfirmedEvent" /> → hide) and Closed (hide) events. Re-populate on a section
    ///     selection change is a direct call from the section subsystem via the Repopulate callback this system hands
    ///     each of them. It owns NO domain logic — it only sequences into the section populators, each of which
    ///     reconciles its own view from ECS (orchestrator + subsystem family, like DistrictOpenConditionSpawnSystem).
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictBuildUISystem : UpdatedSystem, IDisposable
    {
        private readonly EntityStore _world;

        // DI-collected section populators. Ordered once; fixed composition, not per-frame state — hence
        // [StateAllowed] (mirrors DistrictOpenConditionSpawnSystem).
        [StateAllowed]
        private readonly IReadOnlyList<DistrictBuildUISubSystem> _subSystems;

        private readonly ArchetypeQuery _requestedSet;
        private readonly ArchetypeQuery _selectedHexSet;
        private readonly ArchetypeQuery _selectionSet;
        private readonly Archetype _selectionArchetype;

        private DistrictBuildUIView _view;
        private bool _chromeHooked;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildUi;

        public DistrictBuildUISystem(EntityStore world, IReadOnlyList<DistrictBuildUISubSystem> subSystems)
            : base(world.Query<DistrictBuildUIViewComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<UITag>()))
        {
            _world = world;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();

            // Hand each populator the re-populate callback: a section that changes the shared selection (List) calls
            // it to re-run every section against the new state — the direct C# replacement for the re-populate pulse.
            for (var i = 0; i < _subSystems.Count; i++)
                _subSystems[i].Repopulate = PopulateSections;

            _requestedSet = world.Query<DistrictBuildUIRequestedEvent>();
            _selectedHexSet = world.Query<HexSelectedComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexSelectionTag>());
            _selectionSet = world.Query().AllTags(Friflo.Engine.ECS.Tags.Get<DistrictBuildSelectionTag>());
            _selectionArchetype = PresentationUIArchetypes.DistrictBuildSelection(world);
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.GetComponent<DistrictBuildUIViewComponent>().View;
            if (view == null)
                return;

            HookChrome(view);

            if (_requestedSet.Count > 0)
                Open(view);
        }

        // The view outlives this system; subscribe once to its chrome C# events. Handled synchronously in the click
        // callback (main thread), mirroring the section subsystems' own view hooks.
        private void HookChrome(DistrictBuildUIView view)
        {
            if (_chromeHooked)
                return;

            _view = view;
            view.Confirmed += OnConfirmed;
            view.Closed += OnClosed;
            _chromeHooked = true;
        }

        // Confirm: read the selected hex + district from ECS, raise the cross-domain build pulse the Actions assembly
        // consumes (it can't read the Presentation selection), then hide. Confirm builds AND closes.
        private void OnConfirmed()
        {
            var (coords, type) = ReadSelection();
            var payer = ReadPayer();

            _world.CreateEvent(new DistrictBuildConfirmedEvent { Coords = coords, Type = type, Payer = payer });

            _view.Hide();
            DestroySelection();
        }

        // Dismiss (X / scrim): hide only. There is no draft, so nothing to discard.
        private void OnClosed()
        {
            _view.Hide();
            DestroySelection();
        }

        // The overlay is modal, so the selected hex + district cannot change while it is open. Missing either is a
        // broken invariant (the overlay only opens with a selected hex and default-selects a district) — fail loud
        // rather than build an Unknown district.
        private (HexCoord coords, DistrictType type) ReadSelection()
        {
            if (!_selectedHexSet.TryGetFirst(out var hexEntity))
                throw new InvalidOperationException("DistrictBuildUISystem: confirm with no selected hex.");
            if (!_selectionSet.TryGetFirst(out var selectionEntity))
                throw new InvalidOperationException("DistrictBuildUISystem: confirm with no selected district.");

            var coords = hexEntity.GetComponent<HexSelectedComponent>().Coords;
            var type = selectionEntity.GetComponent<DistrictBuildSelectionComponent>().Selected;
            return (coords, type);
        }

        // The chosen payer lives view-local in the price section (its owner, per DistrictBuildPriceUIView). That
        // section always resolves a valid default on populate, so Unknown here means it never populated — a broken
        // invariant, fail loud (mirrors ReadSelection). Captured into the confirmed pulse; BuildDistrictActionSystem
        // spends the payer's stockpile from it (R2).
        private ActorType ReadPayer()
        {
            if (!_world.HasWorldComponent<DistrictBuildPriceUIViewComponent>())
                throw new InvalidOperationException("DistrictBuildUISystem: confirm with no price section view.");

            var payer = _world.GetWorldComponent<DistrictBuildPriceUIViewComponent>().View.SelectedOwner;
            if (payer == ActorType.Unknown)
                throw new InvalidOperationException("DistrictBuildUISystem: confirm with no selected payer.");

            return payer;
        }

        private void Open(DistrictBuildUIView view)
        {
            // The build prompt only exists while a hex is selected; a stray request without one is a no-op.
            if (_selectedHexSet.Count == 0)
                return;

            CreateSelection();
            PopulateSections();
            view.Show();
        }

        // The selection lives on a single local entity for the lifetime of the open overlay: created here on open
        // (the section subsystems write/read DistrictBuildSelectionComponent onto it), destroyed on close.
        // DistrictBuildSelectionComponent is a birth column at DistrictType.Unknown ("nothing picked yet") — the
        // list subsystem overwrites it with a real default the moment it populates.
        private void CreateSelection()
        {
            if (_selectionSet.Count > 0)
                return;

            _selectionArchetype.CreateEntity();
        }

        private void DestroySelection()
        {
            if (_selectionSet.TryGetFirst(out var entity))
                entity.DeleteEntity();
        }

        // Each section subsystem reconciles its own view from the current ECS selection; the orchestrator only
        // sequences them by Priority and hands over the overlay root.
        private void PopulateSections()
        {
            var root = _world.GetWorldComponent<DistrictBuildUIRootComponent>().RootBox.Value;

            for (var i = 0; i < _subSystems.Count; i++)
                if (_subSystems[i].IsEnabled)
                    _subSystems[i].Populate(root);
        }

        public void Dispose()
        {
            if (_chromeHooked && _view != null)
            {
                _view.Confirmed -= OnConfirmed;
                _view.Closed -= OnClosed;
            }
        }
    }
}
