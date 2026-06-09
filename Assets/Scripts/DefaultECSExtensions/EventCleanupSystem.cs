using DefaultEcs;
using JetBrains.Annotations;

namespace DefaultECSExtensions
{
    /// <summary>
    ///     Removes all one-frame event entities (those carrying <see cref="EventTag" />)
    ///     at the end of the update loop. Lives in the shared extensions assembly so any flow/state can
    ///     wire it without creating an assembly cycle.
    /// </summary>
    [UsedImplicitly]
    public sealed class EventCleanupSystem : UpdatedSystem
    {
        private const int ExecutionPriority = int.MaxValue;

        /// <inheritdoc />
        public override int Priority => ExecutionPriority;

        /// <param name="world">The ECS world to query for one-frame event entities.</param>
        public EventCleanupSystem(World world)
            : base(world.GetEntities()
                .With<EventTag>()
                .AsSet())
        {
        }

        /// <inheritdoc />
        protected override void Update(GameState state, in Entity entity)
        {
            entity.Dispose();
        }
    }
}
