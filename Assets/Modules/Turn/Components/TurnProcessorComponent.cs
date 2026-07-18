using Friflo.Engine.ECS;
using Modules.Turn.Data;

namespace Modules.Turn.Components
{
    /// <summary>
    ///     World component present only while a turn is being processed. Doubles as the "turn in progress"
    ///     signal other systems gate on. Set to <see cref="TurnProcessorStatus.Running" /> when a turn starts,
    ///     flipped to <see cref="TurnProcessorStatus.Completed" /> by the background run, then removed by
    ///     <c>TurnProcessorSystem</c>.
    /// </summary>
    public struct TurnProcessorComponent : IComponent
    {
        public TurnProcessorStatus Status;
    }
}
