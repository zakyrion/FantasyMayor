using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Tags;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.Turn.Events;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.UI.MainHud.HexInfoPanel.Configs;
using Presentation.UI.MainHud.HexInfoPanel.Views;
using Presentation.Terrain.Tags;
using Presentation.UI.Tags;
using UnityEngine;

namespace Presentation.UI.MainHud.HexInfoPanel.Systems
{
    /// <summary>
    ///     Drives the District block's state for the selected hex. Reactive on EITHER
    ///     <see cref="SelectedHexChangedEvent" /> (a new hex is selected), <see cref="TurnCompletedEvent" />
    ///     (turns-left on an in-progress build advances while the same hex stays selected), OR
    ///     <see cref="DistrictBuildConfirmedEvent" /> (the player just confirmed a build on the still-selected
    ///     hex — without this trigger the block would keep showing the build prompt until a reselect or the next
    ///     turn boundary) — <c>Priority</c> is deliberately set above ALL THREE producers (see
    ///     SystemPriorities.RuntimeTick.HexInfoPanelDistrict) so any pulse is visible the same frame it is raised.
    ///     With no selection it hides every district block; otherwise it checks — in order — whether the hex has
    ///     an in-progress build (shows type + icon + turns-left + cancel), then whether a district FACT exists
    ///     (District table — key <see cref="HexIdComponent" /> + discriminator <see cref="DistrictTag" />, still
    ///     SCAFFOLD: no backing economy components yet, so this branch is currently unreachable), else the
    ///     build-prompt block.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelDistrictSystem : UpdatedSystem
    {
        private readonly World _world;
        private readonly EntitySet _viewSet;
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _districtSet;
        private readonly EntitySet _inProgressSet;

        private bool _cancelHooked;
        private HexInfoPanelView _cancelHookedView;

        public override int Priority => SystemPriorities.RuntimeTick.HexInfoPanelDistrict;

        public HexInfoPanelDistrictSystem(World world)
            : base(world.GetEntities()
                .WithEither<SelectedHexChangedEvent>().Or<TurnCompletedEvent>().Or<DistrictBuildConfirmedEvent>()
                .AsSet())
        {
            _world = world;
            _viewSet = world.GetEntities().With<HexInfoPanelViewComponent>().With<UITag>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().With<HexSelectionTag>().AsSet();
            _districtSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<DistrictTag>()
                .AsSet();
            _inProgressSet = world.GetEntities()
                .With<BuildDistrictInProgressTag>()
                .With<HexIdComponent>()
                .With<DistrictTypeComponent>()
                .With<BuildDistrictTurnsLeftComponent>()
                .AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (_viewSet.Count == 0)
                return;

            var view = _viewSet.GetEntities()[0].Get<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            HookCancel(view);

            if (_selectedHexSet.Count == 0)
            {
                view.HideDistrict();
                return;
            }

            var coords = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;

            if (TryGetInProgress(coords, out var districtType, out var turnsLeft))
                ShowInProgress(view, districtType, turnsLeft);
            else if (HasDistrict(coords))
                view.ShowDistrictDetails();
            else
                view.ShowDistrictBuildPrompt();
        }

        // Click-frequency lookup → a linear scan over the (currently empty) District table, no maintained index.
        // One district per hex, so the first coordinate match is the answer.
        private bool HasDistrict(HexCoord coords)
        {
            foreach (var districtEntity in _districtSet.GetEntities())
            {
                if (districtEntity.Get<HexIdComponent>().Coords == coords)
                    return true;
            }

            return false;
        }

        // One in-progress build per hex, so the first coordinate match is the answer.
        private bool TryGetInProgress(HexCoord coords, out DistrictType districtType, out int turnsLeft)
        {
            foreach (var inProgressEntity in _inProgressSet.GetEntities())
            {
                if (inProgressEntity.Get<HexIdComponent>().Coords != coords)
                    continue;

                districtType = inProgressEntity.Get<DistrictTypeComponent>().Value;
                turnsLeft = inProgressEntity.Get<BuildDistrictTurnsLeftComponent>().Value;
                return true;
            }

            districtType = default;
            turnsLeft = default;
            return false;
        }

        private void ShowInProgress(HexInfoPanelView view, DistrictType districtType, int turnsLeft)
        {
            if (!_world.Has<DistrictIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexInfoPanelDistrictSystem: DistrictIconConfigComponent is missing.");

            var config = _world.Get<DistrictIconConfigComponent>().Value;
            TryGetDistrictIconEntry(config, districtType, out var sprite, out var displayName);

            view.ShowDistrictInProgress(sprite, displayName, turnsLeft);
        }

        private bool TryGetDistrictIconEntry(DistrictIconConfig config, DistrictType type, out Sprite sprite,
            out string displayName)
        {
            foreach (var entry in config.Entries)
            {
                if (entry.Type != type)
                    continue;

                sprite = entry.Sprite;
                displayName = entry.DisplayName;
                return true;
            }

            sprite = null;
            displayName = type.ToString();
            return false;
        }

        // Hooked lazily (the view is resolved from the EntitySet, not injected) — guarded so a live PanelRenderer
        // reload, which recreates the view's VisualElements but not the MonoBehaviour instance, never double-hooks.
        private void HookCancel(HexInfoPanelView view)
        {
            if (_cancelHooked)
                return;

            view.Cancelled += OnCancelled;
            _cancelHooked = true;
            _cancelHookedView = view;
        }

        // Dormant emitter: wired for R5 (Flows/FLOW_DISTRICT_BUILD.md) — the target BuildDistrictActionCancelSystem
        // does not exist yet, so there is nothing to raise. The subscription exists now so the view's cancel
        // affordance is live and R5 only needs to fill this handler in.
        private void OnCancelled()
        {
        }

        public override void Dispose()
        {
            if (_cancelHooked && _cancelHookedView != null)
                _cancelHookedView.Cancelled -= OnCancelled;

            _viewSet.Dispose();
            _selectedHexSet.Dispose();
            _districtSet.Dispose();
            _inProgressSet.Dispose();
            base.Dispose();
        }
    }
}
