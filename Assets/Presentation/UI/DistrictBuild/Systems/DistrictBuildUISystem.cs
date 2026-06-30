using System.Collections.Generic;
using System.Linq;
using Core;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictOpenCondition.Tags;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Presentation.Terrain.Components;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Events;
using Presentation.UI.DistrictBuild.Views;

namespace Presentation.UI.DistrictBuild.Systems
{
    /// <summary>
    ///     Drives the district-build overlay: visibility + section dispatch. Anchored on the
    ///     DistrictBuildUIViewComponent singleton (ticks once per frame). Coalesces the window pulses —
    ///     <see cref="DistrictBuildRequestedEvent" /> (open), <see cref="DistrictBuildClosedEvent" /> (hide),
    ///     <see cref="DistrictBuildSelectionRequestedEvent" /> (re-select). It owns NO section data: on open it sets
    ///     the default selection, on each selection request it updates <see cref="DistrictBuildSelectionComponent" />,
    ///     then re-runs the section populators — each reconciles its own view from ECS (orchestrator + subsystem
    ///     family, like DistrictOpenConditionSpawnSystem).
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictBuildUISystem : UpdatedSystem
    {
        private const int ExecutionPriority = 565;

        private readonly World _world;

        // DI-collected section populators. Ordered once; fixed composition, not per-frame state — hence
        // [StateAllowed] (mirrors DistrictOpenConditionSpawnSystem).
        [StateAllowed]
        private readonly IReadOnlyList<DistrictBuildUISubSystem> _subSystems;

        private readonly EntitySet _requestedSet;
        private readonly EntitySet _closedSet;
        private readonly EntitySet _selectionRequestedSet;
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _buildableSet;

        public override int Priority => ExecutionPriority;

        public DistrictBuildUISystem(World world, IReadOnlyList<DistrictBuildUISubSystem> subSystems)
            : base(world.GetEntities().With<DistrictBuildUIViewComponent>().AsSet())
        {
            _world = world;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
            _requestedSet = world.GetEntities().With<DistrictBuildRequestedEvent>().AsSet();
            _closedSet = world.GetEntities().With<DistrictBuildClosedEvent>().AsSet();
            _selectionRequestedSet = world.GetEntities().With<DistrictBuildSelectionRequestedEvent>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().AsSet();
            _buildableSet = world.GetEntities()
                .With<DistrictTypeComponent>().With<DistrictCanBeBuildTag>().AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.Get<DistrictBuildUIViewComponent>().View;
            if (view == null)
                return;

            if (_closedSet.Count > 0)
                view.Hide();

            if (_requestedSet.Count > 0)
                Open(view);

            if (_selectionRequestedSet.Count > 0)
                ChangeSelection();
        }

        private void Open(DistrictBuildUIView view)
        {
            // The build prompt only exists while a hex is selected; a stray request without one is a no-op.
            if (_selectedHexSet.Count == 0)
                return;

            _world.Set(new DistrictBuildSelectionComponent { Selected = DefaultSelection() });
            PopulateSections();
            view.Show();
        }

        private void ChangeSelection()
        {
            var district = _selectionRequestedSet.GetEntities()[0]
                .Get<DistrictBuildSelectionRequestedEvent>().District;
            _world.Set(new DistrictBuildSelectionComponent { Selected = district });
            PopulateSections();
        }

        // First buildable district (carrying DistrictCanBeBuildTag); Unknown when none, which clears the sections.
        private DistrictType DefaultSelection() =>
            _buildableSet.Count > 0
                ? _buildableSet.GetEntities()[0].Get<DistrictTypeComponent>().Value
                : DistrictType.Unknown;

        // Each section subsystem reconciles its own view from the current ECS selection; the orchestrator only
        // sequences them by Priority and hands over the overlay root.
        private void PopulateSections()
        {
            var root = _world.Get<DistrictBuildUIRootComponent>().RootBox.Value;

            for (var i = 0; i < _subSystems.Count; i++)
                if (_subSystems[i].IsEnabled)
                    _subSystems[i].Populate(root);
        }

        public override void Dispose()
        {
            _requestedSet.Dispose();
            _closedSet.Dispose();
            _selectionRequestedSet.Dispose();
            _selectedHexSet.Dispose();
            _buildableSet.Dispose();
            base.Dispose();
        }
    }
}
