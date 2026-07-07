namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     One-frame event: the player dismissed the district-build overlay WITHOUT confirming (close button or
    ///     scrim), so the draft build entity must be discarded. Raised on its own entity alongside <c>EventTag</c> by
    ///     <c>DistrictBuildUIView</c> and consumed by <c>BuildDistrictTemplateCancelSystem</c>. Distinct from the
    ///     Presentation-only <c>DistrictBuildClosedEvent</c>, which merely hides the window: dismiss = close (hide) +
    ///     cancel (discard entity); confirm = confirm (promote) + close (hide), never cancel. Payload-less: the
    ///     target is every entity still carrying <c>BuildDistrictActionTemplateTag</c>. Cleared by
    ///     <c>EventCleanupSystem</c>.
    /// </summary>
    public struct DistrictBuildCancelledEvent
    {
    }
}
