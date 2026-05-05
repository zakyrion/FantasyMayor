using DefaultEcs.System;

namespace DefaultECSExtensions
{
    /// <summary>
    ///     Marks a system as a participant in the per-frame Unity Update loop.
    ///     All registered implementations are sorted ascending by <see cref="Priority" /> before the loop starts.
    /// </summary>
    public interface IUpdatedSystem : ISystem<GameState>
    {
        /// <summary>Execution order within the update loop. Lower values run first.</summary>
        int Priority { get; }
    }
}
