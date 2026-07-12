namespace Flows.DistrictBuild.Events
{
    /// <summary>
    ///     One-frame event requesting the district-build overlay be opened for the selected hex. Raised on an
    ///     entity alongside <c>EventTag</c> by the build slot in <c>HexInfoPanelView</c>; payload-less by design
    ///     (pulse + reconcile) — <c>DistrictBuildUISystem</c> reads the current <c>HexSelectedComponent</c> for the
    ///     target hex when it shows the overlay. Lives in the district-build flow assembly (the code home of the
    ///     FLOW_DISTRICT_BUILD contract): a cross-subfeature UI-navigation intent owned by neither HexInfoPanel (the
    ///     emitter) nor DistrictBuild (the consumer).
    /// </summary>
    public struct DistrictBuildUIRequestedEvent
    {
    }
}
