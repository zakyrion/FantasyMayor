namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     One-frame event: a district finished building. Raised on its own entity alongside <c>EventTag</c>,
    ///     one pulse per completion pass. Payload-less — the reactive <c>DistrictViewSpawnSystem</c> reconciles
    ///     against current world state (the built District fact entities, <c>DistrictTag</c>) rather than reading
    ///     a payload. Raised by <c>BuildDistrictCompletionSystem</c> at build completion. Cleared by
    ///     <c>EventCleanupSystem</c>.
    /// </summary>
    public struct DistrictBuiltEvent
    {
    }
}
