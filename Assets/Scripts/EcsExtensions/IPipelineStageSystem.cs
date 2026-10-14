namespace EcsExtensions
{
    /// <summary>
    ///     The system kind of a map-creation stage: a first-order one-shot system that also declares an
    ///     explicit execution order via <see cref="Priority" />, so a sequential pipeline runs its stages
    ///     in a deterministic order (lower runs first), independent of DI registration order.
    /// </summary>
    public interface IPipelineStageSystem : IUniTaskSystem
    {
        /// <summary>Execution order within the pipeline. Lower runs first.</summary>
        int Priority { get; }
    }
}
