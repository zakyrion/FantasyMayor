using Friflo.Engine.ECS;

namespace EcsExtensions
{
    /// <summary>
    ///     Resolves the event archetype for a payload type: [<see cref="EventFrameComponent" />, TPayload]
    ///     tagged <see cref="EventTag" />. Unlike the domain holders this one is generic — this assembly owns
    ///     the frame stamp and the tag, while each payload type belongs to the assembly that raises it, so
    ///     the archetype is closed at the call site by <see cref="EcsEventExtensions.CreateEvent{T}" />.
    ///     This holder stores nothing; the caller keeps the resolved archetype if it needs it repeatedly.
    /// </summary>
    public static class EventArchetypes
    {
        public static Archetype Of<T>(EntityStore store) where T : struct, IComponent =>
            store.GetArchetype(
                ComponentTypes.Get<EventFrameComponent, T>(),
                Tags.Get<EventTag>());
    }
}
