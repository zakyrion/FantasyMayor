using Friflo.Engine.ECS;
using Modules.Turn.Data;

namespace Modules.Turn.Components
{
    /// <summary>
    ///     Permanent world component that records the turn processor lifecycle. <see cref="TurnProcessorStatus.Running" />
    ///     is the "turn in progress" signal; completion returns it to <see cref="TurnProcessorStatus.Idle" />.
    /// </summary>
    public struct TurnProcessorComponent : IComponent
    {
        public TurnProcessorStatus Status;
    }
}
