namespace Modules.Turn.Events
{
    /// <summary>
    ///     One-frame event requesting the next turn be computed. Raised on an entity alongside
    ///     <c>EventTag</c>; consumed by <c>TurnProcessorSystem</c>. The emitter is gameplay-owned and
    ///     not wired yet.
    /// </summary>
    public struct NextTurnEvent
    {
    }
}
