using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Turn.Components;
using Modules.Turn.Events;

namespace Modules.Turn.Systems
{
    /// <summary>
    ///     Advances the turn counter. Reactive on the one-frame <see cref="TurnCompletedEvent" /> pulse raised by
    ///     <see cref="TurnProcessorSystem" /> when a turn resolves — it does NOT poll the processor, so the
    ///     completion-detection logic lives in one place. Priority is above the processor (1000) so the pulse is
    ///     read the same frame it is emitted, before <c>EventCleanupSystem</c> (int.MaxValue) clears it.
    /// </summary>
    [UsedImplicitly]
    public sealed class TurnCountSystem : UpdatedSystem
    {
        private const int ExecutionPriority = 1010;

        private readonly World _world;

        public override int Priority => ExecutionPriority;

        public TurnCountSystem(World world)
            : base(world.GetEntities().With<TurnCompletedEvent>().AsSet())
        {
            _world = world;
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (!_world.Has<TurnCountComponent>())
                throw new InvalidOperationException(
                    "TurnCountSystem: TurnCountComponent is missing — it must be seeded on Gameplay enter.");

            var current = _world.Get<TurnCountComponent>().Value;
            _world.Set(new TurnCountComponent(current + 1));
        }
    }
}
