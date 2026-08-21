namespace EcsExtensions
{
    /// <summary>
    ///     Marks a system as a participant in the per-frame Unity LateUpdate loop.
    ///     All registered implementations are sorted ascending by <see cref="Priority" /> before the loop starts.
    /// </summary>
    public interface ILateUpdatedSystem
    {
        /// <summary>Execution order within the late-update loop. Lower values run first.</summary>
        int Priority { get; }

        /// <summary>Updates the system once for the current frame.</summary>
        void Update(GameState state);
    }
}
