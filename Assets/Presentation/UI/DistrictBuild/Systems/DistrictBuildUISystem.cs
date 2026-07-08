using System.Collections.Generic;
using System.Linq;
using Core;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Presentation.Terrain.Components;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Events;
using Presentation.UI.DistrictBuild.Tags;
using Presentation.UI.DistrictBuild.Views;
using Presentation.Terrain.Tags;
using Presentation.UI.Tags;

namespace Presentation.UI.DistrictBuild.Systems
{
    /// <summary>
    ///     Drives the district-build overlay: visibility + section dispatch. Anchored on the
    ///     DistrictBuildUIViewComponent singleton (ticks once per frame). Coalesces the window pulses —
    ///     <see cref="DistrictBuildRequestedEvent" /> (open), <see cref="DistrictBuildUIClosedEvent" /> (hide),
    ///     <see cref="DistrictBuildSelectedDistrictEvent" /> (re-populate after a selection change). It owns NO
    ///     domain logic — it only sequences pulses into the section populators, each of which reconciles its own view
    ///     from ECS (orchestrator + subsystem family, like DistrictOpenConditionSpawnSystem).
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictBuildUISystem : UpdatedSystem
    {
        private readonly World _world;

        // DI-collected section populators. Ordered once; fixed composition, not per-frame state — hence
        // [StateAllowed] (mirrors DistrictOpenConditionSpawnSystem).
        [StateAllowed]
        private readonly IReadOnlyList<DistrictBuildUISubSystem> _subSystems;

        private readonly EntitySet _requestedSet;
        private readonly EntitySet _closedSet;
        private readonly EntitySet _selectedDistrictEventSet;
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _selectionSet;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildUi;

        public DistrictBuildUISystem(World world, IReadOnlyList<DistrictBuildUISubSystem> subSystems)
            : base(world.GetEntities().With<DistrictBuildUIViewComponent>().With<UITag>().AsSet())
        {
            _world = world;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
            _requestedSet = world.GetEntities().With<DistrictBuildRequestedEvent>().With<EventTag>().AsSet();
            _closedSet = world.GetEntities().With<DistrictBuildUIClosedEvent>().With<EventTag>().AsSet();
            _selectedDistrictEventSet = world.GetEntities().With<DistrictBuildSelectedDistrictEvent>().With<EventTag>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().With<HexSelectionTag>().AsSet();
            _selectionSet = world.GetEntities().With<DistrictBuildSelectionTag>().AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.Get<DistrictBuildUIViewComponent>().View;
            if (view == null)
                return;

            if (_closedSet.Count > 0)
            {
                view.Hide();
                DestroySelection();
            }

            if (_requestedSet.Count > 0)
                Open(view);
            else if (_selectedDistrictEventSet.Count > 0)
                PopulateSections();
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
        private void CreateSelection()
        {
            if (_selectionSet.Count > 0)
                return;

            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuildSelectionTag());
        }

        private void DestroySelection()
        {
            foreach (var entity in _selectionSet.GetEntities())
                entity.Dispose();
        }

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
            _selectedDistrictEventSet.Dispose();
            _selectedHexSet.Dispose();
            _selectionSet.Dispose();
            base.Dispose();
        }
    }
}
