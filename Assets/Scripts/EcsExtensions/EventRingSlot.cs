namespace EcsExtensions
{
    /// <summary>The id of one living event entity, in its type's ring.</summary>
    internal readonly struct EventRingSlot
    {
        public readonly int EntityId;

        public EventRingSlot(int entityId)
        {
            EntityId = entityId;
        }
    }
}
