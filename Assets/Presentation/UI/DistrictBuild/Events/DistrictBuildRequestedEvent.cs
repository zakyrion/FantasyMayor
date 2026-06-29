namespace Presentation.UI.DistrictBuild.Events
{
    /// <summary>
    ///     One-frame event requesting the district-build window be opened for the selected hex. Raised on an
    ///     entity alongside <c>EventTag</c> by the build slot in <c>HexInfoPanelView</c>; payload-less by design
    ///     (pulse + reconcile) — <c>DistrictBuildActionSystem</c> reads the current <c>HexSelectedComponent</c>
    ///     for the target hex when it shows the window.
    /// </summary>
    public struct DistrictBuildRequestedEvent
    {
    }
}
