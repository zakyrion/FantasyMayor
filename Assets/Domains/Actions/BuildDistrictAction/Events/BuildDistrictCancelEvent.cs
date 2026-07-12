using Modules.AxialSystem;

namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     One-frame event: the player clicked Cancel on the in-progress-build block for the selected hex. The view
    ///     raises the local C# Cancelled event; <c>HexInfoPanelDistrictSystem</c> handles it and raises this on its
    ///     own entity alongside <c>EventTag</c>. CARRIES the hex — the identifying value the reactive
    ///     <c>BuildDistrictActionCancelSystem</c> needs to find the right in-progress entity (PATTERN_EVENT: tiny
    ///     identifying value, tolerated when the target can't be derived from state). The payload crosses the
    ///     asmdef boundary because the Actions assembly cannot read the Presentation selection state, same
    ///     rationale as <c>DistrictBuildConfirmedEvent</c>. Cleared by <c>EventCleanupSystem</c>.
    /// </summary>
    public struct BuildDistrictCancelEvent
    {
        public HexCoord Coords;
    }
}
