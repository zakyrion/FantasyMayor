namespace Presentation.UI.DistrictBuild.Events
{
    // One-frame pulse: the player picked a district row. Payload-less by design (pulse + reconcile) —
    // DistrictBuildListUISubSystem already wrote DistrictBuildSelectionComponent before raising this; it only
    // tells the orchestrator to re-run PopulateSections() for the other section subsystems.
    public struct DistrictBuildSelectionRequestedEvent
    {
    }
}
