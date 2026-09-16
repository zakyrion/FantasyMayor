using EcsExtensions;
namespace Modules.Turn.Events
{
    /// <summary>
    ///     Event requesting the next turn be computed. Consumed by <c>TurnProcessorSystem</c>, which drains its
    ///     reader once per tick and reacts once when the batch was non-empty. The emitter is gameplay-owned and
    ///     not wired yet.
    /// </summary>
    public struct NextTurnEvent : IEventTag
    {
    }
}
