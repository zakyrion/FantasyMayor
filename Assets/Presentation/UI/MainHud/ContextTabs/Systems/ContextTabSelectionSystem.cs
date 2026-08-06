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
    ///     ActiveContextTabComponent (both the view and the active tab are singleton components the view wrote on
    ///     click). Keeping the restyle in a system (not the MonoBehaviour) mirrors the TurnPanelView/TurnPanelViewSystem
    ///     split. Idempotent. Anchored on the event set, mirroring the HexInfoPanel block systems.
    /// </summary>
    [UsedImplicitly]
    public sealed class ContextTabSelectionSystem : UpdatedSystem
    {
        private readonly EntityStorages _storages;

        public override int Priority => SystemPriorities.RuntimeTick.ContextTabSelection;

        public ContextTabSelectionSystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<ContextTabChangedEvent>(storages.World))
        {
            _storages = storages;
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (!EcsEventExtensions.IsRipe(entity))
                return;

            if (!_storages.Singletons.Has<ContextTabsViewComponent>())
                return;

            if (!_storages.Singletons.Has<ActiveContextTabComponent>())
                throw new InvalidOperationException(
                    "ContextTabSelectionSystem: ActiveContextTabComponent is missing — it must be seeded on spawn.");

            var active = _storages.Singletons.Get<ActiveContextTabComponent>().Value;
            if (active == ContextTab.Unknown)
                throw new InvalidOperationException(
                    "ContextTabSelectionSystem: active tab is Unknown — the view must record a real tab.");

            var view = _storages.Singletons.Get<ContextTabsViewComponent>().View;
            if (view == null)
                return;

            view.SetActive(active);
        }
    }
}
