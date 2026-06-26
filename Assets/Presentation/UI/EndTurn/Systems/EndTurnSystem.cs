using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Presentation.UI.EndTurn.Components;
using Modules.Turn.Components;

namespace Presentation.UI.EndTurn.Systems
{
    /// <summary>
    ///     Drives the turn corner each Gameplay frame and owns the bottom-panel shell reveal: reveals the whole
    ///     panel (it spawns hidden) since the turn corner is its always-present part, reflects whether a turn is
    ///     running — Processing while a <see cref="TurnProcessorComponent" /> exists, Ready otherwise — and
    ///     pushes the current turn number from <see cref="TurnCountComponent" /> into "Хід N". The
    ///     click→NextTurnEvent emit lives in the view; this only mirrors engine state. Stateless: Show,
    ///     SetProcessing, and SetTurnNumber are idempotent. Anchored on the button-view singleton so it ticks
    ///     once per frame, mirroring HexInfoPanelSystem.
    /// </summary>
    [UsedImplicitly]
    public sealed class EndTurnSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 560;

        private readonly World _world;

        public override int Priority => ExecutionPriority;

        public EndTurnSystem(World world)
            : base(world.GetEntities().With<EndTurnViewComponent>().AsSet())
        {
            _world = world;
        }

        protected override void Update(GameState state, in Entity entity)
        {
            var view = entity.Get<EndTurnViewComponent>().View;
            if (view == null)
                return;

            if (!_world.Has<TurnCountComponent>())
                throw new InvalidOperationException(
                    "EndTurnSystem: TurnCountComponent is missing — it must be seeded on Gameplay enter.");

            view.Show();
            view.SetProcessing(_world.Has<TurnProcessorComponent>());
            view.SetTurnNumber(_world.Get<TurnCountComponent>().Value);
        }
    }
}
