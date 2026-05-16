using DefaultEcs.System;

namespace DefaultECSExtensions
{
    /// <summary>
    ///     Marks a system as a participant in the per-frame Unity LateUpdate loop.
    ///     All registered implementations are sorted ascending by <see cref="Priority" /> before the loop starts.
    /// </summary>
    public interface ILateUpdatedSystem : ISystem<GameState>
    {
        /// <summary>Execution order within the late-update loop. Lower values run first.</summary>
        int Priority { get; }
    }
}
