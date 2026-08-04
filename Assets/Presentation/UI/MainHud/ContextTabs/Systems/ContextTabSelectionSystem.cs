using System;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.UI.MainHud.ContextTabs.Components;
using Presentation.UI.MainHud.ContextTabs.Data;
using Presentation.UI.MainHud.ContextTabs.Events;

namespace Presentation.UI.MainHud.ContextTabs.Systems
{
    /// <summary>
    ///     Reactive on the payload-less ContextTabChangedEvent pulse: reconciles the view against the current
    ///     ActiveContextTabComponent (both the view and the active tab are world singletons the view wrote on
    ///     click). Keeping the restyle in a system (not the MonoBehaviour) mirrors the TurnPanelView/TurnPanelViewSystem
    ///     split. Idempotent. Anchored on the event set, mirroring the HexInfoPanel block systems.
    /// </summary>
    [UsedImplicitly]
    public sealed class ContextTabSelectionSystem : UpdatedSystem
    {
        private readonly EntityStore _world;

        public override int Priority => SystemPriorities.RuntimeTick.ContextTabSelection;

        public ContextTabSelectionSystem(EntityStore world)
            : base(world, EventArchetypes.Of<ContextTabChangedEvent>(world))
        {
            _world = world;
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (!EcsEventExtensions.IsRipe(entity))
                return;

            if (!_world.HasWorldComponent<ContextTabsViewComponent>())
                return;

            if (!_world.HasWorldComponent<ActiveContextTabComponent>())
                throw new InvalidOperationException(
                    "ContextTabSelectionSystem: ActiveContextTabComponent is missing — it must be seeded on spawn.");

            var active = _world.GetWorldComponent<ActiveContextTabComponent>().Value;
            if (active == ContextTab.Unknown)
                throw new InvalidOperationException(
                    "ContextTabSelectionSystem: active tab is Unknown — the view must record a real tab.");

            var view = _world.GetWorldComponent<ContextTabsViewComponent>().View;
            if (view == null)
                return;

            view.SetActive(active);
        }
    }
}
