using Friflo.Engine.ECS;
namespace Modules.Turn.Events
{
    /// <summary>
    ///     One-frame event announcing that a turn has fully resolved (the <c>TurnProcessorComponent</c> was just
    ///     removed). Raised on an entity alongside <c>EventTag</c> by <c>TurnProcessorSystem</c>; consumed by
    ///     <c>TurnCountSystem</c> (and any future turn-boundary reactors). Decouples the turn counter from the
    ///     processor's completion-detection logic.
    /// </summary>
    public struct TurnCompletedEvent : IComponent
    {
    }
}
