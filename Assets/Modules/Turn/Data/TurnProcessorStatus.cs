namespace Modules.Turn.Data
{
    /// <summary>
    ///     Lifecycle of the turn currently being processed. Default is <see cref="Running" />, so a freshly
    ///     set <c>TurnProcessorComponent</c> represents an in-flight turn.
    /// </summary>
    public enum TurnProcessorStatus
    {
        Running,
        Completed
    }
}
