namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     One-frame event: the player confirmed «Збудувати» in the district-build overlay. Raised on its own
    ///     entity alongside <c>EventTag</c> by <c>DistrictBuildUIView</c> — separate from the
    ///     <c>DistrictBuildClosedEvent</c> that hides the window (build and close are two events). Payload-less
    ///     pulse: the confirmed choice is read from world state (<c>DistrictBuildSelectionComponent</c> +
    ///     <c>HexSelectedComponent</c>) by the reactive-orchestrator <c>BuildDistrictActionSystem</c>. Cleared by
    ///     <c>EventCleanupSystem</c>.
    /// </summary>
    public struct DistrictBuildConfirmedEvent
    {
    }
}
