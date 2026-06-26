using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Presentation.UI.HexInfoPanel.Components;
using Presentation.Terrain.Events;

namespace Presentation.UI.HexInfoPanel.Systems
{
    /// <summary>
    ///     SCAFFOLD. The District-economy blocks have no backing ECS components yet — District / Owner /
    ///     Operator / Workforce / Yield live only at the GAMEPLAY_FOUNDATION level. This placeholder keeps both
    ///     the District kvgrid and the "Вихід цього ходу" yield split hidden on every refresh (one call —
    ///     <see cref="Views.HexInfoPanelView.SetDistrictVisible" /> toggles both) until those components exist;
    ///     replace it with the real district subsystem when they land.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelDistrictPlaceholderSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 562;

        private readonly EntitySet _viewSet;

        public override int Priority => ExecutionPriority;

        public HexInfoPanelDistrictPlaceholderSystem(World world)
            : base(world.GetEntities().With<SelectedHexChangedEvent>().AsSet())
        {
            _viewSet = world.GetEntities().With<HexInfoPanelViewComponent>().AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (_viewSet.Count == 0)
                return;

            var view = _viewSet.GetEntities()[0].Get<HexInfoPanelViewComponent>().View;
            if (view == null)
                return;

            view.SetDistrictVisible(false);
        }

        public override void Dispose()
        {
            _viewSet.Dispose();
            base.Dispose();
        }
    }
}
