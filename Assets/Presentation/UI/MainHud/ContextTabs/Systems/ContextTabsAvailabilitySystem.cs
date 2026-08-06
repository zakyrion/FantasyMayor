using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.Archetypes;
using Presentation.Terrain.Events;
using Presentation.UI.MainHud.ContextTabs.Components;
using Presentation.UI.MainHud.ContextTabs.Data;

namespace Presentation.UI.MainHud.ContextTabs.Systems
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
        private readonly EntityStorages _storages;
        private readonly Archetype _selectedHexSet;

        public override int Priority => SystemPriorities.RuntimeTick.ContextTabsAvailability;

        public ContextTabsAvailabilitySystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<SelectedHexChangedEvent>(storages.World))
        {
            _storages = storages;
            _selectedHexSet = PresentationArchetypes.HexSelection(storages.World);
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (!EcsEventExtensions.IsRipe(entity))
                return;

            if (!_storages.Singletons.Has<ContextTabsViewComponent>())
                return;

            var view = _storages.Singletons.Get<ContextTabsViewComponent>().View;
            if (view == null)
                return;

            var hasSelection = _selectedHexSet.Count > 0;
            view.SetTabEnabled(ContextTab.Overview, IsAvailable(ContextTab.Overview, hasSelection));
            view.SetTabEnabled(ContextTab.Buildings, IsAvailable(ContextTab.Buildings, hasSelection));
            view.SetTabEnabled(ContextTab.Actions, IsAvailable(ContextTab.Actions, hasSelection));
        }

        // STUB: no player-action model yet — every tab is always available regardless of selection.
        private bool IsAvailable(ContextTab tab, bool hasSelection)
        {
            return true;
        }
    }
}
