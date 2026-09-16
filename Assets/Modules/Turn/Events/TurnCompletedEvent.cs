using EcsExtensions;
namespace Modules.Turn.Events
{
    /// <summary>
    ///     Event announcing that a turn has fully resolved (the <c>TurnProcessorComponent</c> was just
    ///     removed). Raised by <c>TurnProcessorSystem</c>; consumed by <c>TurnCountSystem</c> (and any future
    ///     turn-boundary reactors). Decouples the turn counter from the processor's completion-detection logic.
    /// </summary>
    public struct TurnCompletedEvent : IEventTag
    {
    }
}
