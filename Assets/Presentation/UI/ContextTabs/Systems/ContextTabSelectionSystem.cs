using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Presentation.UI.ContextTabs.Components;
using Presentation.UI.ContextTabs.Data;
using Presentation.UI.ContextTabs.Events;

namespace Presentation.UI.ContextTabs.Systems
{
    /// <summary>
    ///     Reactive on the payload-less ContextTabChangedEvent pulse: reconciles the view against the current
    ///     ActiveContextTabComponent (both the view and the active tab are world singletons the view wrote on
    ///     click). Keeping the restyle in a system (not the MonoBehaviour) mirrors the EndTurnView/EndTurnViewSystem
    ///     split. Idempotent. Anchored on the event set, mirroring the HexInfoPanel block systems.
    /// </summary>
    [UsedImplicitly]
    public sealed class ContextTabSelectionSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 560;

        private readonly World _world;

        public override int Priority => ExecutionPriority;

        public ContextTabSelectionSystem(World world)
            : base(world.GetEntities().With<ContextTabChangedEvent>().AsSet())
        {
            _world = world;
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (!_world.Has<ContextTabsViewComponent>())
                return;

            if (!_world.Has<ActiveContextTabComponent>())
                throw new InvalidOperationException(
                    "ContextTabSelectionSystem: ActiveContextTabComponent is missing — it must be seeded on spawn.");

            var active = _world.Get<ActiveContextTabComponent>().Value;
            if (active == ContextTab.Unknown)
                throw new InvalidOperationException(
                    "ContextTabSelectionSystem: active tab is Unknown — the view must record a real tab.");

            var view = _world.Get<ContextTabsViewComponent>().View;
            if (view == null)
                return;

            view.SetActive(active);
        }
    }
}
