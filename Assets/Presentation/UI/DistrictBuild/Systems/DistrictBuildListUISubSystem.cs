using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictOpenCondition.Tags;
using JetBrains.Annotations;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Events;
using UnityEngine;

namespace Presentation.UI.DistrictBuild.Systems
{
    // Projects the open-condition table into the build list: every entity carrying DistrictCanBeBuildTag is a
    // district the player may build. Read-only on the table — a future evaluator owns the tag. Marks the current
    // DistrictBuildSelectionComponent as active, and translates the view's row-click (local C# event) into the
    // DistrictBuildSelectionRequestedEvent pulse (the view stays World-free).
    [UsedImplicitly]
    public sealed class DistrictBuildListUISubSystem : DistrictBuildUISubSystem
    {
        private const int ExecutionPriority = 100;

        private readonly EntitySet _buildable;
        private bool _hooked;

        public override int Priority => ExecutionPriority;

        public DistrictBuildListUISubSystem(World world) : base(world)
        {
            _buildable = world.GetEntities()
                .With<DistrictTypeComponent>()
                .With<DistrictCanBeBuildTag>()
                .AsSet();
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

            var selected = World.Has<DistrictBuildSelectionComponent>()
                ? World.Get<DistrictBuildSelectionComponent>().Selected
                : DistrictType.Unknown;

            view.Clear();
            foreach (var entity in _buildable.GetEntities())
            {
                var type = entity.Get<DistrictTypeComponent>().Value;
                view.AddDistrict(type, type == selected);
            }
        }

        private void OnSelected(DistrictType district)
        {
            var entity = World.CreateEntity();
            entity.Set(new DistrictBuildSelectionRequestedEvent { District = district });
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
            base.Dispose();
        }
    }
}
