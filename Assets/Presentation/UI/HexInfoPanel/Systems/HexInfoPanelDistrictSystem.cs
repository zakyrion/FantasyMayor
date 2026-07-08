using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.District.Tags;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using Presentation.UI.HexInfoPanel.Components;
using Presentation.Terrain.Tags;
using Presentation.UI.Tags;

namespace Presentation.UI.HexInfoPanel.Systems
{
    /// <summary>
    ///     Drives the District block's state for the selected hex. Reactive on
    ///     <see cref="SelectedHexChangedEvent" />: with no selection it hides every district block; otherwise it
    ///     checks whether a district exists on that hex (the District table — key <see cref="HexIdComponent" /> +
    ///     discriminator <see cref="DistrictTag" />) and shows the build-prompt block when none exists, the
    ///     district-details blocks when one does. District details are still SCAFFOLD (no backing economy
    ///     components yet), so the details branch is currently unreachable — nothing spawns a district.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelDistrictSystem : UpdatedSystem
    {
        private readonly EntitySet _viewSet;
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _districtSet;

        public override int Priority => SystemPriorities.RuntimeTick.HexInfoPanelDistrict;

        public HexInfoPanelDistrictSystem(World world)
            : base(world.GetEntities().With<SelectedHexChangedEvent>().AsSet())
        {
            _viewSet = world.GetEntities().With<HexInfoPanelViewComponent>().With<UITag>().AsSet();
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().With<HexSelectionTag>().AsSet();
            _districtSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<DistrictTag>()
                .AsSet();
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
                view.HideDistrict();
                return;
            }

            var coords = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;

            if (HasDistrict(coords))
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

        public override void Dispose()
        {
            _viewSet.Dispose();
            _selectedHexSet.Dispose();
            _districtSet.Dispose();
            base.Dispose();
        }
    }
}
