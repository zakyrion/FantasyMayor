using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.MainUI.EndTurn.Components;
using Modules.Turn.Components;

namespace Modules.MainUI.EndTurn.Systems
{
    /// <summary>
    ///     Drives the end-turn button each Gameplay frame: reveals it (it spawns hidden) and reflects whether a
    ///     turn is running — Processing while a <see cref="TurnProcessorComponent" /> exists, Ready otherwise.
    ///     The click→NextTurnEvent emit lives in the view; this only mirrors engine state. Stateless: Show and
    ///     SetProcessing are idempotent. Anchored on the button-view singleton so it ticks once per frame,
    ///     mirroring HexInfoPanelSystem.
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

            view.Show();
            view.SetProcessing(_world.Has<TurnProcessorComponent>());
        }
    }
}
