using Domains.Economy.District.Data;

namespace Presentation.UI.DistrictBuild.Events
{
    // One-frame pulse: the player picked a district row. Carries the requested district; the orchestrator updates
    // DistrictBuildSelectionComponent and re-populates the sections. Raised by DistrictBuildListUISubSystem.
    public struct DistrictBuildSelectionRequestedEvent
    {
        public DistrictType District;
    }
}
