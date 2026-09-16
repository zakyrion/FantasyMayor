using EcsExtensions;
namespace Presentation.HexResources.Events
{
    /// <summary>
    ///     Generic signal: forest resources changed (something may have appeared). Carries no payload —
    ///     <see cref="Systems.ForestSpawnSystem" /> reconciles the whole forest state against the view state,
    ///     so the signal only needs to *exist*. Lives in the event log until evicted past its ring capacity.
    ///     No emitter wires it yet — runtime planting is future gameplay; the consumer is a dormant scaffold.
    /// </summary>
    public struct ForestHexAppearedEvent : IEventTag
    {
    }
}
