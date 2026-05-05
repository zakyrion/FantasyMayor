using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Installers.World
{
    /// <summary>
    ///     Removes all one-frame event entities at the end of the update loop.
    /// </summary>
    [UsedImplicitly]
    internal sealed class EventCleanupSystem : UpdatedSystem
    {
        private const int ExecutionPriority = int.MaxValue;

        /// <inheritdoc />
        public override int Priority => ExecutionPriority;

        /// <param name="world">The ECS world to query for one-frame event entities.</param>
        public EventCleanupSystem(DefaultEcs.World world)
            : base(world.GetEntities()
                .With<EventMarkerComponent>()
                .AsSet())
        {
        }

        /// <inheritdoc />
        protected override void Update(GameState state, in Entity entity)
        {
            Debug.Log("[skh] Updating event cleanup");
            entity.Dispose();
        }
    }
}
