namespace Modules.Turn.Data
{
    /// <summary>
    ///     Lifecycle of the turn processor. Default is <see cref="Idle" />.
    /// </summary>
    public enum TurnProcessorStatus
    {
        Idle,
        Running,
        Completed
    }
}
