namespace Domains.Actions.BuildDistrictAction.Events
{
    // One-frame pulse: "discard the pending district-build draft" — raised by the DistrictBuild UI when the
    // window closes without committing (Presentation → Actions; the UI raise-site is a later slice).
    // BuildDistrictDraftDiscardSystem disposes the draft. Payload-less; cleared by EventCleanupSystem.
    public struct BuildDistrictActionDiscardRequestedEvent
    {
    }
}
