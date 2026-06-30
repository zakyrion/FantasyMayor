using Domains.Economy.District.Data;

namespace Presentation.UI.DistrictBuild.Components
{
    // World component: the currently selected district in the open build overlay. Written by the orchestrator
    // (default on open, then on each selection request) and read + reconciled by the section subsystems.
    public struct DistrictBuildSelectionComponent
    {
        public DistrictType Selected;
    }
}
