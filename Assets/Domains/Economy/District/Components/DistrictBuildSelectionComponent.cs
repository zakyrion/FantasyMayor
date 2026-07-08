using Domains.Economy.District.Data;

namespace Domains.Economy.District.Components
{
    // World component: the currently selected district in the open build overlay. Written exclusively by
    // DistrictBuildListUISubSystem (default-selected on window-open, then on each row click), read + reconciled by
    // the other section subsystems, and read by BuildDistrictActionSystem on confirm as the source of truth for the
    // district being built. Lives in Economy (not Presentation) so the Actions domain can read it without inverting
    // the Presentation -> Actions dependency.
    public struct DistrictBuildSelectionComponent
    {
        public DistrictType Selected;
    }
}
