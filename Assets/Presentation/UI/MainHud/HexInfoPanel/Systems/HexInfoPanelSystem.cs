using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using Presentation.Terrain.Tags;
using Presentation.UI.Tags;

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
        private readonly ArchetypeQuery _viewSet;
        private readonly ArchetypeQuery _selectedHexSet;
        private readonly ArchetypeQuery _hexSet;

        public override int Priority => SystemPriorities.RuntimeTick.HexInfoPanel;

        public HexInfoPanelSystem(EntityStore world)
            : base(world.Query<SelectedHexChangedEvent>())
        {
            _viewSet = world.Query<HexInfoPanelViewComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<UITag>());
            _selectedHexSet = world.Query<HexSelectedComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexSelectionTag>());
            _hexSet = world.Query<HexIdComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexTag>());
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
