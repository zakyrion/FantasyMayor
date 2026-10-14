using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Data;
using EcsExtensions;
using Flows.DistrictBuild.Events;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.UI.Archetypes;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Tags;
using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Systems
{
    // Projects the open-condition table into the build list: every entity whose DistrictOpenStateComponent
    // reads Buildable is a district the player may build. Read-only on the table — the evaluator subsystem
    // owns the state. Sole owner of DistrictBuildSelectionComponent: default-selects the first buildable
    // district on a non-empty batch of the overlay-open event, writes the selection directly on row-click, and
    // marks the current selection as active. On the view's row-click (local C# event) it writes the selection,
    // then calls the orchestrator-provided Repopulate to re-run the other section subsystems (the view stays
    // World-free). Holds its own reader (:orchestrator/own-reader) — the orchestrator forwards nothing to it.
    [UsedImplicitly]
    [ViewSubscriber(typeof(DistrictBuildListUIView))]
    public sealed class DistrictBuildListUISubSystem : DistrictBuildUISubSystem
    {
        private readonly EntityStorages _storages;
        private readonly EventReader<DistrictBuildUIRequestedEvent> _overlayRequests;
        private readonly ComponentIndex<DistrictOpenStateComponent, DistrictOpenState> _buildable;
        private readonly Archetype _selectionSet;
        private bool _hooked;

        public override int Priority => SystemPriorities.SubSystems.DistrictBuildUi.List;

        public DistrictBuildListUISubSystem(EntityStorages storages, EventReader<DistrictBuildUIRequestedEvent> overlayRequests) : base(storages.World)
        {
            _storages = storages;
            _overlayRequests = overlayRequests;
            _buildable = storages.World.ComponentIndex<DistrictOpenStateComponent, DistrictOpenState>();
            _selectionSet = PresentationUIArchetypes.DistrictBuildSelection(storages.World);
        }

        public override UniTask Update(CancellationToken cancellationToken)
        {
            var view = _storages.Singletons.Get<DistrictBuildListUIViewComponent>().View;

            // The view outlives the subsystem; subscribe once to the local row-click event.
            if (!_hooked)
            {
                view.SelectionChanged += OnSelected;
                _hooked = true;
            }

            var buildable = _buildable[DistrictOpenState.Buildable];

            if (!_selectionSet.TryGetFirst(out var selectionEntity))
                throw new InvalidOperationException(
                    "DistrictBuildListUISubSystem: Populate called with no active " +
                    $"{nameof(DistrictBuildSelectionTag)} entity.");

            // The window just opened this pass: default-select the first buildable district (None if the list
            // is empty), before this Populate call (and the other sections') reads it. A non-empty batch means
            // the overlay opened since the reader last drained — a Repopulate call from a row click finds the
            // reader already drained here, so the pick it just wrote is not reset (H4).
            if (_overlayRequests.DrainBatch())
            {
                var defaultSelection = buildable.Count > 0
                    ? buildable[0].GetComponent<DistrictTypeFKComponent>().Value
                    : DistrictType.None;

                selectionEntity.AddComponent(new DistrictBuildSelectionComponent { Selected = defaultSelection });
            }

            // Always set by this point (default-selected above on open, or already present from a prior
            // open/click) — a missing component here is a bug, so let Get throw rather than fall back.
            var selected = selectionEntity.GetComponent<DistrictBuildSelectionComponent>().Selected;

            view.Clear();
            foreach (var entity in buildable)
            {
                var type = entity.GetComponent<DistrictTypeFKComponent>().Value;
                view.AddDistrict(type, type == selected);
            }

            return UniTask.CompletedTask;
        }

        private void OnSelected(DistrictType district)
        {
            // Fail loud with context (mirrors DistrictBuildUISystem.ReadSelection/ReadPayer): a row click firing
            // with no active selection entity means the overlay closed/confirmed between the click and this
            // handler running — root cause not yet pinned down (2026-07-17); replaces a bare IndexOutOfRangeException.
            if (!_selectionSet.TryGetFirst(out var selectionEntity))
                throw new InvalidOperationException(
                    $"DistrictBuildListUISubSystem: row click for '{district}' fired with no active " +
                    $"{nameof(DistrictBuildSelectionTag)} entity — the overlay closed/confirmed before this handler ran.");

            selectionEntity.AddComponent(new DistrictBuildSelectionComponent { Selected = district });

            // Selection written — re-run every section populator (including this one, to re-mark the active row)
            // against the new selection. Direct C# call into the orchestrator, ordered after the write.
            Repopulate?.Invoke();
        }

        public override void Dispose()
        {
            if (_hooked && _storages.Singletons.Has<DistrictBuildListUIViewComponent>())
            {
                var view = _storages.Singletons.Get<DistrictBuildListUIViewComponent>().View;
                if (view != null)
                    view.SelectionChanged -= OnSelected;
            }

            base.Dispose();
        }
    }
}
