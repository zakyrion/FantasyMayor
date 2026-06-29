namespace Presentation.UI.DistrictBuild.Events
{
    /// <summary>
    ///     One-frame event requesting the district-build window be closed. Raised on an entity alongside
    ///     <c>EventTag</c> by <c>DistrictBuildActionView</c> on the close «X», the scrim, or the «Збудувати»
    ///     button (build is a dormant later slice — for now «Збудувати» only closes). Payload-less;
    ///     <c>DistrictBuildActionSystem</c> hides the window on this pulse. Cleared by <c>EventCleanupSystem</c>.
    /// </summary>
    public struct DistrictBuildClosedEvent
    {
    }
}
