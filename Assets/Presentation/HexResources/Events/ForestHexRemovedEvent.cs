using EcsExtensions;
namespace Presentation.HexResources.Events
{
    /// <summary>
    ///     Generic signal: forest resources changed (something may have been removed). Carries no payload —
    ///     <see cref="Systems.ForestDespawnSystem" /> reconciles forest views against the current forest-
    ///     resource state, so the signal only needs to *exist*. Lives in the event log until evicted past its
    ///     ring capacity. No emitter wires it yet — runtime chopping is future gameplay; the consumer is a
    ///     dormant scaffold.
    /// </summary>
    public struct ForestHexRemovedEvent : IEventTag
    {
    }
}
