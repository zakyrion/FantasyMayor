using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
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
    ///     <see cref="DistrictTableChangedEvent" /> (a District row entered a new stage — confirm, completion, and
    ///     cancel all fold into this one pulse) — <c>Priority</c> is deliberately set above ALL THREE producers
    ///     (see SystemPriorities.RuntimeTick.HexInfoPanelDistrict) so any pulse is visible the same frame it is
    ///     raised. With no selection it hides every district block; otherwise it resolves the selected hex's
    ///     District row (FLOW_DISTRICT_BUILD unification, 2026-07-17): no row → build-prompt block; row staged
    ///     <c>DistrictBuildState.Planned</c> → in-progress block (type + icon + turns-left, read off the matching
    ///     verb row via <c>DistrictIdFKComponent</c> + cancel); row staged <c>Built</c> → district-details block.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelDistrictSystem : UpdatedSystem
    {
        private readonly World _world;
        private readonly EntitySet _viewSet;
        private readonly EntitySet _selectedHexSet;

        // District rows indexed by hex — one district per hex, so the first match is the answer.
        private readonly EntityMultiMap<HexIdFKComponent> _districtsByHex;

        // In-progress verb rows indexed by their FK into the District PK space.
        private readonly EntityMultiMap<DistrictIdFKComponent> _inProgressByDistrictId;

        private bool _cancelHooked;
        private HexInfoPanelView _cancelHookedView;

        public override int Priority => SystemPriorities.RuntimeTick.HexInfoPanelDistrict;

        public HexInfoPanelDistrictSystem(World world)
            : base(world.GetEntities()
                .WithEither<SelectedHexChangedEvent>().Or<TurnCompletedEvent>().Or<DistrictTableChangedEvent>()
                .AsSet())
        {
            _world = world;
            _viewSet = world.GetEntities().With<HexInfoPanelViewComponent>().With<UITag>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().With<HexSelectionTag>().AsSet();
            _districtsByHex = world.GetEntities()
                .With<DistrictTag>()
                .With<HexIdFKComponent>()
                .With<DistrictIdComponent>()
                .With<DistrictTypeComponent>()
                .With<DistrictBuildStateComponent>()
                .AsMultiMap<HexIdFKComponent>();
            _inProgressByDistrictId = world.GetEntities()
                .With<BuildDistrictInProgressTag>()
                .With<DistrictIdFKComponent>()
                .With<BuildDistrictTurnsComponent>()
                .AsMultiMap<DistrictIdFKComponent>();
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

            if (!TryGetDistrict(coords, out var district))
            {
                view.ShowDistrictBuildPrompt();
                return;
            }

            if (district.Get<DistrictBuildStateComponent>().Value == DistrictBuildState.Planned)
            {
                var turnsLeft = ResolveTurnsLeft(district.Get<DistrictIdComponent>().Value);
                ShowInProgress(view, district.Get<DistrictTypeComponent>().Value, turnsLeft);
            }
            else
            {
                view.ShowDistrictDetails();
            }
        }

        private bool TryGetDistrict(HexCoord coords, out Entity district)
        {
            if (_districtsByHex.TryGetEntities(new HexIdFKComponent { Coords = coords }, out var matches) && matches.Length > 0)
            {
                district = matches[0];
                return true;
            }

            district = default;
            return false;
        }

        // A Planned District row always has exactly one matching verb row — a miss is a broken invariant.
        private int ResolveTurnsLeft(int districtId)
        {
            if (!_inProgressByDistrictId.TryGetEntities(new DistrictIdFKComponent { Value = districtId }, out var matches) || matches.Length == 0)
                throw new InvalidOperationException(
                    $"HexInfoPanelDistrictSystem: no in-progress verb row for Planned district {districtId}.");

            return matches[0].Get<BuildDistrictTurnsComponent>().TurnsLeft;
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

        // Raises BuildDistrictCancelEvent for the selected hex's in-progress build. Guarded, not fail-loud: the
        // cancel button only exists while the block shows the in-progress state, but a defensive re-check costs
        // nothing against a stale-dispatch race. BuildDistrictActionCancelSystem owns the actual state mutation
        // and is the one that fails loud if the pulse ever reaches it without a matching hex.
        private void OnCancelled()
        {
            if (_selectedHexSet.Count == 0)
                return;

            var coords = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;
            if (!TryGetDistrict(coords, out var district) || district.Get<DistrictBuildStateComponent>().Value != DistrictBuildState.Planned)
                return;

            var pulse = _world.CreateEntity();
            pulse.Set(new BuildDistrictCancelEvent { Coords = coords });
            pulse.Set(new EventTag());
        }

        public override void Dispose()
        {
            if (_cancelHooked && _cancelHookedView != null)
                _cancelHookedView.Cancelled -= OnCancelled;

            _viewSet.Dispose();
            _selectedHexSet.Dispose();
            _districtsByHex.Dispose();
            _inProgressByDistrictId.Dispose();
            base.Dispose();
        }
    }
}
