namespace EcsExtensions
{
    /// <summary>
    ///     An <see cref="IUniTaskSystem{T}" /> that declares an explicit execution order via <see cref="Priority" />.
    ///     Used when a sequential async pipeline must run its systems in a deterministic order
    ///     (lower priority runs first), independent of DI registration order.
    /// </summary>
    /// <typeparam name="T">The type of the object used as state to update the system.</typeparam>
    public interface IPrioritizedUniTaskSystem<in T> : IUniTaskSystem<T>
    {
        /// <summary>Execution order within the pipeline. Lower runs first.</summary>
        int Priority { get; }
    }
}
