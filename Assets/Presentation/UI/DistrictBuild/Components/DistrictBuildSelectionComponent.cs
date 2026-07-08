using Domains.Economy.District.Data;

namespace Presentation.UI.DistrictBuild.Components
{
    // Local component (Presentation-only): the currently selected district in the open build overlay. Rides on a
    // single entity tagged DistrictBuildSelectionTag, created on overlay-open and destroyed on close by
    // DistrictBuildUISystem. Written exclusively by DistrictBuildListUISubSystem (default-selected on open, then on
    // each row click), read + reconciled by the other section subsystems. Not visible outside this subdomain.
    public struct DistrictBuildSelectionComponent
    {
        public DistrictType Selected;
    }
}
