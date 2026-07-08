namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     One-frame event: a district finished building on its hex. Raised on its own entity alongside
    ///     <c>EventTag</c>, one pulse per built hex. Payload-less — the reactive <c>DistrictViewSpawnSystem</c>
    ///     reconciles against current world state (the committed <c>BuildDistrictActionTag</c> district entities)
    ///     rather than reading a payload. Raised by <c>BuildDistrictActionSystem</c> when a build is confirmed.
    ///     Cleared by <c>EventCleanupSystem</c>.
    /// </summary>
    public struct DistrictBuiltEvent
    {
    }
}
