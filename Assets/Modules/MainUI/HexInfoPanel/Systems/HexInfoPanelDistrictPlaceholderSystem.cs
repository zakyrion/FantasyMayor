using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.MainUI.HexInfoPanel.Components;
using Modules.MainUI.HexInfoPanel.Events;

namespace Modules.MainUI.HexInfoPanel.Systems
{
    /// <summary>
    ///     SCAFFOLD. The district block has no backing ECS components yet — District / Owner / Operator /
    ///     Workforce / Yield live only at the GAMEPLAY_FOUNDATION level. This placeholder keeps the block
    ///     hidden on every refresh until those components exist; replace it with the real district subsystem
    ///     when they land.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexInfoPanelDistrictPlaceholderSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 562;

        private readonly EntitySet _viewSet;

        public override int Priority => ExecutionPriority;

        public HexInfoPanelDistrictPlaceholderSystem(World world)
            : base(world.GetEntities().With<HexInfoPanelRefreshEvent>().AsSet())
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
