using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Economy.DistrictOpenCondition.Data;
using Domains.Economy.DistrictOpenCondition.Tags;
using Flows.DistrictBuild.Events;
using JetBrains.Annotations;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Tags;
using UnityEngine;

namespace Presentation.UI.DistrictBuild.Systems
{
    // Projects the open-condition table into the build list: every entity whose DistrictOpenStateComponent
    // reads Buildable is a district the player may build. Read-only on the table — the evaluator subsystem
    // owns the state. Sole owner of DistrictBuildSelectionComponent: default-selects the first buildable
    // district on the window-open pulse, writes the selection directly on row-click, and marks the current
    // selection as active. On the view's row-click (local C# event) it writes the selection, then calls the
    // orchestrator-provided Repopulate to re-run the other section subsystems (the view stays World-free).
    [UsedImplicitly]
    public sealed class DistrictBuildListUISubSystem : DistrictBuildUISubSystem
    {
        private readonly EntityMultiMap<DistrictOpenStateComponent> _buildable;
        private readonly EntitySet _requestedSet;
        private readonly EntitySet _selectionSet;
        private bool _hooked;

        public override int Priority => SystemPriorities.SubSystems.DistrictBuildUi.List;

        public DistrictBuildListUISubSystem(World world) : base(world)
        {
            _buildable = world.GetEntities()
                .With<DistrictOpenConditionTag>()
                .AsMultiMap<DistrictOpenStateComponent>();
            _requestedSet = world.GetEntities().With<DistrictBuildUIRequestedEvent>().AsSet();
            _selectionSet = world.GetEntities().With<DistrictBuildSelectionTag>().AsSet();
        }

        public override void Populate(GameObject root)
        {
            var view = World.Get<DistrictBuildListUIViewComponent>().View;

            // The view outlives the subsystem; subscribe once to the local row-click event.
            if (!_hooked)
            {
                view.SelectionChanged += OnSelected;
                _hooked = true;
            }

            var buildable = _buildable.TryGetEntities(
                new DistrictOpenStateComponent { Value = DistrictOpenState.Buildable }, out var buildableRows)
                ? buildableRows
                : ReadOnlySpan<Entity>.Empty;

            // The window just opened this tick: default-select the first buildable district (None if the list
            // is empty), before this Populate call (and the other sections') reads it.
            if (_requestedSet.Count > 0)
            {
                var defaultSelection = buildable.Length > 0
                    ? buildable[0].Get<DistrictTypeFKComponent>().Value
                    : DistrictType.None;

                _selectionSet.GetEntities()[0].Set(new DistrictBuildSelectionComponent { Selected = defaultSelection });
            }

            // Always set by this point (default-selected above on open, or already present from a prior
            // open/click) — a missing component here is a bug, so let Get throw rather than fall back.
            var selected = _selectionSet.GetEntities()[0].Get<DistrictBuildSelectionComponent>().Selected;

            view.Clear();
            foreach (var entity in buildable)
            {
                var type = entity.Get<DistrictTypeFKComponent>().Value;
                view.AddDistrict(type, type == selected);
            }
        }

        private void OnSelected(DistrictType district)
        {
            _selectionSet.GetEntities()[0].Set(new DistrictBuildSelectionComponent { Selected = district });

            // Selection written — re-run every section populator (including this one, to re-mark the active row)
            // against the new selection. Direct C# call into the orchestrator, ordered after the write.
            Repopulate?.Invoke();
        }

        public override void Dispose()
        {
            if (_hooked && World.Has<DistrictBuildListUIViewComponent>())
            {
                var view = World.Get<DistrictBuildListUIViewComponent>().View;
                if (view != null)
                    view.SelectionChanged -= OnSelected;
            }

            _buildable.Dispose();
            _requestedSet.Dispose();
            _selectionSet.Dispose();
            base.Dispose();
        }
    }
}
