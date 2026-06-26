using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Presentation.UI.ContextTabs.Components;
using Presentation.UI.ContextTabs.Data;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;

namespace Presentation.UI.ContextTabs.Systems
{
    /// <summary>
    ///     Reconciles per-tab availability from the current selection. Reactive on the
    ///     <see cref="SelectedHexChangedEvent" /> pulse (raised by HexSelectionSystem): on each change it pushes
    ///     <c>view.SetTabEnabled(tab, IsAvailable(tab, hasSelection))</c> for the real tabs, reading the world
    ///     <see cref="ContextTabsViewComponent" /> and whether a <see cref="HexSelectedComponent" /> exists.
    ///     Idempotent. The pre-selection state is the UIElements default (all enabled), so no spawn seed is needed.
    ///     STUB: there is no player-action model yet, so IsAvailable returns true — this carries the mechanism
    ///     (now keyed to selection); replace it with real rules (action points, ownership, turn phase) later.
    /// </summary>
    [UsedImplicitly]
    public sealed class ContextTabsAvailabilitySystem : UpdatedSystem
    {
        private const int ExecutionPriority = 560;

        private readonly World _world;
        private readonly EntitySet _selectedHexSet;

        public override int Priority => ExecutionPriority;

        public ContextTabsAvailabilitySystem(World world)
            : base(world.GetEntities().With<SelectedHexChangedEvent>().AsSet())
        {
            _world = world;
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (!_world.Has<ContextTabsViewComponent>())
                return;

            var view = _world.Get<ContextTabsViewComponent>().View;
            if (view == null)
                return;

            var hasSelection = _selectedHexSet.Count > 0;
            view.SetTabEnabled(ContextTab.Overview, IsAvailable(ContextTab.Overview, hasSelection));
            view.SetTabEnabled(ContextTab.Buildings, IsAvailable(ContextTab.Buildings, hasSelection));
            view.SetTabEnabled(ContextTab.Actions, IsAvailable(ContextTab.Actions, hasSelection));
        }

        public override void Dispose()
        {
            _selectedHexSet.Dispose();
            base.Dispose();
        }

        // STUB: no player-action model yet — every tab is always available regardless of selection.
        private bool IsAvailable(ContextTab tab, bool hasSelection)
        {
            return true;
        }
    }
}
