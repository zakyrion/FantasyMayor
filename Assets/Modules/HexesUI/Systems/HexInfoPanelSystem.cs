using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.HexCore.Components;
using Modules.HexCore.Tags;
using Modules.HexesUI.Components;
using Modules.HexesUI.Events;
using Modules.TerrainView.Components;

namespace Modules.HexesUI.Systems
{
    /// <summary>
    ///     Drives the hex info panel from the current selection. Shows the panel and raises a one-frame
    ///     <see cref="HexInfoPanelRefreshEvent" /> when the selected hex changes; hides it when nothing — or a
    ///     coordinate with no hex — is selected. Per-block systems react to the refresh event.
    ///     Anchored on the panel-view singleton so it ticks once per frame, mirroring HexSelectionViewSystem.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 550;

        private readonly World _world;
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _hexSet;

        private bool _isShown;
        private bool _hasProcessed;
        private HexCoord _lastCoords;

        public override int Priority => ExecutionPriority;

        public HexInfoPanelSystem(World world)
            : base(world.GetEntities().With<HexInfoPanelViewComponent>().AsSet())
        {
            _world = world;
            _selectedHexSet = world.GetEntities().With<SelectedHexComponent>().AsSet();
            _hexSet = world.GetEntities().With<HexTag>().With<HexIdComponent>().AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.Get<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            if (_selectedHexSet.Count == 0)
            {
                if (_isShown)
                {
                    view.Hide();
                    _isShown = false;
                }

                _hasProcessed = false;
                return;
            }

            var coords = _selectedHexSet.GetEntities()[0].Get<SelectedHexComponent>().Coords;
            if (_hasProcessed && _lastCoords == coords)
                return;

            _hasProcessed = true;
            _lastCoords = coords;

            // A click can land on a coordinate with no hex (e.g. outside the grid) — that is not a real hex,
            // so the panel stays hidden rather than showing an empty header.
            if (!HexExists(coords))
            {
                if (_isShown)
                {
                    view.Hide();
                    _isShown = false;
                }

                return;
            }

            view.Show();
            RaiseRefresh(coords);
            _isShown = true;
        }

        private bool HexExists(HexCoord coords)
        {
            foreach (var hexEntity in _hexSet.GetEntities())
            {
                if (hexEntity.Get<HexIdComponent>().Coords == coords)
                    return true;
            }

            return false;
        }

        private void RaiseRefresh(HexCoord coords)
        {
            var refreshEntity = _world.CreateEntity();
            refreshEntity.Set(new HexInfoPanelRefreshEvent { Coords = coords });
            refreshEntity.Set(new EventTag());
        }

        public override void Dispose()
        {
            _selectedHexSet.Dispose();
            _hexSet.Dispose();
            base.Dispose();
        }
    }
}
