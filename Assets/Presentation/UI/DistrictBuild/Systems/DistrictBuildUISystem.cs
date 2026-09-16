using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Data;
using Domains.Kernel.Data;
using EcsExtensions;
using Flows.DistrictBuild.Events;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.Boot.Core;
using Presentation.Archetypes;
using Presentation.Terrain.Components;
using Presentation.UI.Archetypes;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Systems
{
    /// <summary>
    ///     Drives the district-build overlay: visibility + section dispatch. Anchored on the
    ///     DistrictBuildUIViewComponent singleton (ticks once per frame). Opens on a non-empty batch of
    ///     <see cref="DistrictBuildUIRequestedEvent" /> (raised by HexInfoPanel). Chrome is C#-event driven: it
    ///     subscribes to the view's Confirmed (read the ECS selection → raise the cross-domain
    ///     <see cref="DistrictBuildConfirmedEvent" /> → hide) and Closed (hide) events. Re-populate on a section
    ///     selection change is a direct call from the section subsystem via the Repopulate callback this system hands
    ///     each of them. It owns NO domain logic — it only keeps its sections through the sub-system contract
    ///     (<see cref="OrchestratorSubSystems" />) and runs them; each section reconciles its own view from ECS
    ///     (orchestrator + subsystem family, like DistrictOpenConditionSpawnSystem).
    /// </summary>
    [UsedImplicitly]
    [SystemRole(SystemRoleKind.PerFrame)]
    [ViewSubscriber(typeof(DistrictBuildUIView))]
    public sealed class DistrictBuildUISystem : UpdatedSystem, IDisposable
    {
        private readonly EntityStorages _storages;

        // Kept sub-systems of this orchestrator, in ascending Priority — fixed composition, not per-frame state,
        // hence [StateAllowed] (mirrors DistrictOpenConditionSpawnSystem).
        [StateAllowed]
        private readonly IReadOnlyList<IPrioritizedUniTaskSystem> _subSystems;

        private readonly EventReader<DistrictBuildUIRequestedEvent> _overlayRequests;
        private readonly Archetype _selectedHexSet;
        private readonly Archetype _selectionArchetype;

        private DistrictBuildUIView _view;
        private bool _chromeHooked;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildUi;

        public DistrictBuildUISystem(
            AppState appState, EntityStorages storages, EventReader<DistrictBuildUIRequestedEvent> overlayRequests,
            IReadOnlyList<IPrioritizedUniTaskSystem> allSubSystems)
            : base(appState, storages.World, PresentationUIArchetypes.DistrictBuildUI(storages.World))
        {
            _storages = storages;
            _overlayRequests = overlayRequests;
            _subSystems = OrchestratorSubSystems.SelectForOrchestrator(typeof(DistrictBuildUISystem), allSubSystems);

            // Hand each populator the re-populate callback: a section that changes the shared selection (List) calls
            // it to re-run every section against the new state — the direct C# replacement for the re-populate pulse.
            HandRepopulateToSections();

            _selectedHexSet = PresentationArchetypes.HexSelection(storages.World);
            _selectionArchetype = PresentationUIArchetypes.DistrictBuildSelection(storages.World);
        }

        // Every kept part must be a district-build section — only that family names this host as its
        // OrchestratorType, so anything else here is a mistyped OrchestratorType elsewhere.
        private void HandRepopulateToSections()
        {
            foreach (var part in _subSystems)
            {
                var section = part as DistrictBuildUISubSystem;
                if (section == null)
                    throw new InvalidOperationException(
                        $"DistrictBuildUISystem: kept a sub-system of type {part.GetType().Name} that is not a {nameof(DistrictBuildUISubSystem)}.");

                section.Repopulate = PopulateSections;
            }
        }

        protected override void Update(GameState state, in Entity entity)
        {
            // Drained before the view guard so a request never lingers unread while the view is briefly missing.
            var overlayRequested = _overlayRequests.DrainBatch();

            var view = entity.GetComponent<DistrictBuildUIViewComponent>().View;
            if (view == null)
                return;

            HookChrome(view);

            if (overlayRequested)
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

        // Confirm: read the selected hex + district from ECS, raise the cross-domain build event the Actions assembly
        // consumes (it can't read the Presentation selection), then hide. Confirm builds AND closes.
        private void OnConfirmed()
        {
            var (coords, type) = ReadSelection();
            var payer = ReadPayer();

            _storages.Events.Raise(new DistrictBuildConfirmedEvent { Coords = coords, Type = type, Payer = payer });

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
            if (!_selectionArchetype.TryGetFirst(out var selectionEntity))
                throw new InvalidOperationException("DistrictBuildUISystem: confirm with no selected district.");

            var coords = hexEntity.GetComponent<HexSelectedComponent>().Coords;
            var type = selectionEntity.GetComponent<DistrictBuildSelectionComponent>().Selected;
            return (coords, type);
        }

        // The chosen payer lives view-local in the price section (its owner, per DistrictBuildPriceUIView). That
        // section always resolves a valid default on populate, so Unknown here means it never populated — a broken
        // invariant, fail loud (mirrors ReadSelection). Captured into the confirmed event; BuildDistrictActionSystem
        // spends the payer's stockpile from it (R2).
        private ActorType ReadPayer()
        {
            if (!_storages.Singletons.Has<DistrictBuildPriceUIViewComponent>())
                throw new InvalidOperationException("DistrictBuildUISystem: confirm with no price section view.");

            var payer = _storages.Singletons.Get<DistrictBuildPriceUIViewComponent>().View.SelectedOwner;
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
            if (_selectionArchetype.Count > 0)
                return;

            _selectionArchetype.CreateEntity();
        }

        private void DestroySelection()
        {
            if (_selectionArchetype.TryGetFirst(out var entity))
                entity.DeleteEntity();
        }

        // Runs every enabled section in Priority order through the sub-system contract, and requires the run to
        // finish inside this call: no section awaits today, so a main-thread run completes synchronously — a
        // pending run means a section started waiting, which this host cannot yet keep and poll on its own tick
        // (decision :c13-async-parts-in-update-host). The Status check comes first because GetResult on a pending
        // UniTask throws and returns its source to the pool.
        private void PopulateSections()
        {
            var run = OrchestratorSubSystems.RunAsync(_subSystems, StatusMonitor.Token);

            if (run.Status == UniTaskStatus.Pending)
                throw new InvalidOperationException(
                    "DistrictBuildUISystem: a section awaited past PopulateSections — every section must complete synchronously.");

            run.GetAwaiter().GetResult();
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
