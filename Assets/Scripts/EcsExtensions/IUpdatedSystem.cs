namespace EcsExtensions
{
    /// <summary>
    ///     Marks a system as a participant in the per-frame Unity Update loop.
    ///     All registered implementations are sorted ascending by <see cref="Priority" /> before the loop starts.
    /// </summary>
    public interface IUpdatedSystem
    {
        /// <summary>Execution order within the update loop. Lower values run first.</summary>
        int Priority { get; }

        /// <summary>Updates the system once for the current frame.</summary>
        void Update(GameState state);
    }
}
