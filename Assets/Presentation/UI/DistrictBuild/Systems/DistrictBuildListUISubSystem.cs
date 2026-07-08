using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictOpenCondition.Tags;
using JetBrains.Annotations;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Events;
using Presentation.UI.DistrictBuild.Tags;
using UnityEngine;

namespace Presentation.UI.DistrictBuild.Systems
{
    // Projects the open-condition table into the build list: every entity carrying DistrictCanBeBuildTag is a
    // district the player may build. Read-only on the table — a future evaluator owns the tag. Sole owner of
    // DistrictBuildSelectionComponent: default-selects the first buildable district on the window-open pulse,
    // writes the selection directly on row-click, and marks the current selection as active. Translates the
    // view's row-click (local C# event) into the (payload-less) DistrictBuildSelectionRequestedEvent pulse so the
    // orchestrator re-runs the other section subsystems (the view stays World-free).
    [UsedImplicitly]
    public sealed class DistrictBuildListUISubSystem : DistrictBuildUISubSystem
    {
        private readonly EntitySet _buildable;
        private readonly EntitySet _requestedSet;
        private readonly EntitySet _selectionSet;
        private bool _hooked;

        public override int Priority => SystemPriorities.SubSystems.DistrictBuildUi.List;

        public DistrictBuildListUISubSystem(World world) : base(world)
        {
            _buildable = world.GetEntities()
                .With<DistrictTypeComponent>()
                .With<DistrictCanBeBuildTag>()
                .AsSet();
            _requestedSet = world.GetEntities().With<DistrictBuildRequestedEvent>().AsSet();
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

            // The window just opened this tick: default-select the first buildable district (None if the list
            // is empty), before this Populate call (and the other sections') reads it.
            if (_requestedSet.Count > 0)
            {
                var defaultSelection = _buildable.Count > 0
                    ? _buildable.GetEntities()[0].Get<DistrictTypeComponent>().Value
                    : DistrictType.None;

                _selectionSet.GetEntities()[0].Set(new DistrictBuildSelectionComponent { Selected = defaultSelection });
            }

            // Always set by this point (default-selected above on open, or already present from a prior
            // open/click) — a missing component here is a bug, so let Get throw rather than fall back.
            var selected = _selectionSet.GetEntities()[0].Get<DistrictBuildSelectionComponent>().Selected;

            view.Clear();
            foreach (var entity in _buildable.GetEntities())
            {
                var type = entity.Get<DistrictTypeComponent>().Value;
                view.AddDistrict(type, type == selected);
            }
        }

        private void OnSelected(DistrictType district)
        {
            _selectionSet.GetEntities()[0].Set(new DistrictBuildSelectionComponent { Selected = district });

            var entity = World.CreateEntity();
            entity.Set(new DistrictBuildSelectedDistrictEvent());
            entity.Set(new EventTag());
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
