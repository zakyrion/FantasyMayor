namespace EcsExtensions
{
    /// <summary>The living event entity's id, paired with the global sequence number it was raised under.</summary>
    internal readonly struct EventRingSlot
    {
        public readonly int EntityId;
        public readonly long Sequence;

        public EventRingSlot(int entityId, long sequence)
        {
            EntityId = entityId;
            Sequence = sequence;
        }
    }
}
