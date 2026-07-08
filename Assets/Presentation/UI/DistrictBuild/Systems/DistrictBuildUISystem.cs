using System.Collections.Generic;
using System.Linq;
using Core;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Presentation.Terrain.Components;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Events;
using Presentation.UI.DistrictBuild.Views;
using Presentation.Terrain.Tags;
using Presentation.UI.Tags;

namespace Presentation.UI.DistrictBuild.Systems
{
    /// <summary>
    ///     Drives the district-build overlay: visibility + section dispatch. Anchored on the
    ///     DistrictBuildUIViewComponent singleton (ticks once per frame). Coalesces the window pulses —
    ///     <see cref="DistrictBuildRequestedEvent" /> (open), <see cref="DistrictBuildClosedEvent" /> (hide),
    ///     <see cref="DistrictBuildSelectionRequestedEvent" /> (re-populate after a selection change). On open it
    ///     also emits <see cref="DistrictBuildStartedEvent" /> — the Actions-layer pulse carrying the selected hex +
    ///     district that spawns the draft build entity. It owns NO domain logic and never touches — it only sequences
    ///     pulses into the section populators, each of which reconciles its own view from ECS (orchestrator +
    ///     subsystem family, like DistrictOpenConditionSpawnSystem).
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
        private readonly EntitySet _selectionRequestedSet;
        private readonly EntitySet _selectedHexSet;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildUi;

        public DistrictBuildUISystem(World world, IReadOnlyList<DistrictBuildUISubSystem> subSystems)
            : base(world.GetEntities().With<DistrictBuildUIViewComponent>().With<UITag>().AsSet())
        {
            _world = world;
            _subSystems = subSystems
                .OrderBy(system => system.Priority)
                .ToArray();
            _requestedSet = world.GetEntities().With<DistrictBuildRequestedEvent>().AsSet();
            _closedSet = world.GetEntities().With<DistrictBuildClosedEvent>().AsSet();
            _selectionRequestedSet = world.GetEntities().With<DistrictBuildSelectionRequestedEvent>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().With<HexSelectionTag>().AsSet();
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
            else if (_selectionRequestedSet.Count > 0)
                PopulateSections();
        }

        private void Open(DistrictBuildUIView view)
        {
            // The build prompt only exists while a hex is selected; a stray request without one is a no-op.
            if (_selectedHexSet.Count == 0)
                return;

            RaiseStarted();
            PopulateSections();
            view.Show();
        }

        // Hands the selected hex + district to the Actions layer as a pulse payload so it can spawn the draft build
        // entity: the build domain can't read the Presentation selection directly (that would invert the
        // Presentation → Actions assembly dependency). District is Unknown until the player picks one from the list.
        private void RaiseStarted()
        {
            var coords = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;
            var type = _world.Has<DistrictBuildSelectionComponent>()
                ? _world.Get<DistrictBuildSelectionComponent>().Selected
                : DistrictType.Unknown;

            var entity = _world.CreateEntity();
            entity.Set(new DistrictBuildStartedEvent { Coords = coords, Type = type });
            entity.Set(new EventTag());
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
            _selectionRequestedSet.Dispose();
            _selectedHexSet.Dispose();
            base.Dispose();
        }
    }
}
