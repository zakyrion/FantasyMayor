using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Archetypes;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using Presentation.UI.Archetypes;
using Presentation.UI.MainHud.HexInfoPanel.Components;

namespace Presentation.UI.MainHud.HexInfoPanel.Systems
{
    /// <summary>
    ///     Owns the CONTEXT sub-panel's show/hide. Reactive on the <see cref="SelectedHexChangedEvent" /> pulse
    ///     (raised by HexSelectionSystem on every selection mutation): swaps the panel to the filled blocks when a
    ///     real hex is selected, back to the empty placeholder on deselection or a coordinate with no hex.
    ///     Stateless reconcile (idempotent). The block CONTENT is filled by the per-block systems, which react to
    ///     the same pulse and read HexSelectedComponent; the initial empty state is seeded by
    ///     HexInfoPanelSpawnSubSystem. Does NOT show/hide the bottom-panel shell (TurnPanelViewSystem owns that).
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelSystem : UpdatedSystem
    {
        private readonly Archetype _viewSet;
        private readonly Archetype _selectedHexSet;
        private readonly Archetype _hexSet;

        public override int Priority => SystemPriorities.RuntimeTick.HexInfoPanel;

        public HexInfoPanelSystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<SelectedHexChangedEvent>(storages.World))
        {
            _viewSet = PresentationUIArchetypes.HexInfoPanel(storages.World);
            _selectedHexSet = PresentationArchetypes.HexSelection(storages.World);
            _hexSet = MapArchetypes.Hex(storages.World);
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

            if (!_selectedHexSet.TryGetFirst(out var selectedHexEntity))
            {
                view.ShowEmpty();
                return;
            }

            var coords = selectedHexEntity.GetComponent<HexSelectedComponent>().Coords;

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
            foreach (var hexEntity in _hexSet.Entities)
            {
                if (hexEntity.GetComponent<HexIdComponent>().Coords == coords)
                    return true;
            }

            return false;
        }
    }
}
