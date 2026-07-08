namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     One-frame event: the player confirmed «Збудувати» in the district-build overlay. Raised on its own
    ///     entity alongside <c>EventTag</c> by <c>DistrictBuildUIView</c> — separate from the
    ///     <c>DistrictBuildClosedEvent</c> that hides the window (build and close are two events). Payload-less
    ///     pulse: on it the reactive <c>BuildDistrictActionSystem</c> promotes the draft build entity; the district
    ///     being built is read from the draft's own <c>DistrictTypeComponent</c> (stamped at open), not from
    ///     Presentation selection state. Cleared by <c>EventCleanupSystem</c>.
    /// </summary>
    public struct DistrictBuildConfirmedEvent
    {
    }
}
