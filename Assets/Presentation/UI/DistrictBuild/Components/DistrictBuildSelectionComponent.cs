using Domains.Economy.District.Data;

namespace Presentation.UI.DistrictBuild.Components
{
    // World component: the currently selected district in the open build overlay. Written exclusively by
    // DistrictBuildListUISubSystem (default-selected on window-open, then on each row click) and read +
    // reconciled by the other section subsystems.
    public struct DistrictBuildSelectionComponent
    {
        public DistrictType Selected;
    }
}
