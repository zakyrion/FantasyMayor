using EcsExtensions;
using Modules.AxialSystem;

namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     Event: the player clicked Cancel on the in-progress-build block for the selected hex. The view raises
    ///     the local C# Cancelled event; <c>HexInfoPanelDistrictSystem</c> handles it and raises this event.
    ///     CARRIES the hex — the identifying value the reactive <c>BuildDistrictActionCancelSystem</c> needs to
    ///     find the right in-progress entity (PATTERN_EVENT: tiny identifying value, tolerated when the target
    ///     can't be derived from state). The payload crosses the asmdef boundary because the Actions assembly
    ///     cannot read the Presentation selection state, same rationale as <c>DistrictBuildConfirmedEvent</c>.
    /// </summary>
    public struct BuildDistrictCancelEvent : IEventTag
    {
        public HexCoord Coords;
    }
}
