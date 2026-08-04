using System;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.Archetypes;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Map.Hex.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.Turn.Events;
using Presentation.Archetypes;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using Presentation.UI.Archetypes;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.UI.MainHud.HexInfoPanel.Configs;
using Presentation.UI.MainHud.HexInfoPanel.Views;
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
    public sealed class HexInfoPanelDistrictSystem : UpdatedSystem, IDisposable
    {
        private readonly EntityStore _world;
        private readonly Archetype _viewSet;
        private readonly Archetype _selectedHexSet;

        // District rows: HexIdFKComponent is shared by every hex-anchored entity kind (views, containers,
        // resources), so a bare ComponentIndex over it is ambiguous across kinds — scope to the archetype.
        private readonly Archetype _districts;

        // In-progress verb rows indexed by their FK into the District PK space.
        private readonly ComponentIndex<DistrictIdFKComponent, int> _inProgressByDistrictId;

        private bool _cancelHooked;
        private HexInfoPanelView _cancelHookedView;

        public override int Priority => SystemPriorities.RuntimeTick.HexInfoPanelDistrict;

        public HexInfoPanelDistrictSystem(EntityStore world)
            : base(world.Query()
                .AnyComponents(ComponentTypes.Get<SelectedHexChangedEvent, TurnCompletedEvent, DistrictTableChangedEvent>()))
        {
            _world = world;
            _viewSet = PresentationUIArchetypes.HexInfoPanel(world);
            _selectedHexSet = PresentationArchetypes.HexSelection(world);
            _districts = EconomyArchetypes.District(world);
            _inProgressByDistrictId = world.ComponentIndex<DistrictIdFKComponent, int>();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (!EcsEventExtensions.IsRipe(entity))
                return;

            if (!_viewSet.TryGetFirst(out var viewEntity))
                return;

            var view = viewEntity.GetComponent<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            HookCancel(view);

            if (!_selectedHexSet.TryGetFirst(out var selectedHexEntity))
            {
                view.HideDistrict();
                return;
            }

            var coords = selectedHexEntity.GetComponent<HexSelectedComponent>().Coords;

            if (!TryGetDistrict(coords, out var district))
            {
                view.ShowDistrictBuildPrompt();
                return;
            }

            if (district.GetComponent<DistrictBuildStateComponent>().Value == DistrictBuildState.Planned)
            {
                var turnsLeft = ResolveTurnsLeft(district.GetComponent<DistrictIdComponent>().Value);
                ShowInProgress(view, district.GetComponent<DistrictTypeComponent>().Value, turnsLeft);
            }
            else
            {
                view.ShowDistrictDetails();
            }
        }

        private bool TryGetDistrict(HexCoord coords, out Entity district)
        {
            foreach (var candidate in _districts.Entities)
            {
                if (!candidate.GetComponent<HexIdFKComponent>().Coords.Equals(coords))
                    continue;

                district = candidate;
                return true;
            }

            district = default;
            return false;
        }

        // A Planned District row always has exactly one matching verb row — a miss is a broken invariant.
        private int ResolveTurnsLeft(int districtId)
        {
            if (!_inProgressByDistrictId[districtId].TryGetFirst(out var verb))
                throw new InvalidOperationException(
                    $"HexInfoPanelDistrictSystem: no in-progress verb row for Planned district {districtId}.");

            return verb.GetComponent<BuildDistrictTurnsComponent>().TurnsLeft;
        }

        private void ShowInProgress(HexInfoPanelView view, DistrictType districtType, int turnsLeft)
        {
            if (!_world.HasWorldComponent<DistrictIconConfigComponent>())
                throw new InvalidOperationException(
                    "HexInfoPanelDistrictSystem: DistrictIconConfigComponent is missing.");

            var config = _world.GetWorldComponent<DistrictIconConfigComponent>().Value;
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
            if (!_selectedHexSet.TryGetFirst(out var selectedHexEntity))
                return;

            var coords = selectedHexEntity.GetComponent<HexSelectedComponent>().Coords;
            if (!TryGetDistrict(coords, out var district) || district.GetComponent<DistrictBuildStateComponent>().Value != DistrictBuildState.Planned)
                return;

            _world.CreateEvent(new BuildDistrictCancelEvent { Coords = coords });
        }

        public void Dispose()
        {
            if (_cancelHooked && _cancelHookedView != null)
                _cancelHookedView.Cancelled -= OnCancelled;
        }
    }
}
