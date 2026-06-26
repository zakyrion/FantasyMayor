using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Presentation.UI.HexInfoPanel.Components;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;

namespace Presentation.UI.HexInfoPanel.Systems
{
    /// <summary>
    ///     Owns the CONTEXT sub-panel's show/hide. Reactive on the <see cref="SelectedHexChangedEvent" /> pulse
    ///     (raised by HexSelectionSystem on every selection mutation): swaps the panel to the filled blocks when a
    ///     real hex is selected, back to the empty placeholder on deselection or a coordinate with no hex.
    ///     Stateless reconcile (idempotent). The block CONTENT is filled by the per-block systems, which react to
    ///     the same pulse and read HexSelectedComponent; the initial empty state is seeded by
    ///     HexInfoPanelSpawnSubSystem. Does NOT show/hide the bottom-panel shell (EndTurnSystem owns that).
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 550;

        private readonly EntitySet _viewSet;
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _hexSet;

        public override int Priority => ExecutionPriority;

        public HexInfoPanelSystem(World world)
            : base(world.GetEntities().With<SelectedHexChangedEvent>().AsSet())
        {
            _viewSet = world.GetEntities().With<HexInfoPanelViewComponent>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().AsSet();
            _hexSet = world.GetEntities().With<HexTag>().With<HexIdComponent>().AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (_viewSet.Count == 0)
                return;

            var view = _viewSet.GetEntities()[0].Get<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            if (_selectedHexSet.Count == 0)
            {
                view.ShowEmpty();
                return;
            }

            var coords = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;

            // A click can land on a coordinate with no hex (e.g. outside the grid) — not a real hex, so the
            // panel stays empty rather than showing an empty header.
            if (!HexExists(coords))
            {
                view.ShowEmpty();
                return;
            }

            view.ShowSelection();
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

        public override void Dispose()
        {
            _viewSet.Dispose();
            _selectedHexSet.Dispose();
            _hexSet.Dispose();
            base.Dispose();
        }
    }
}
