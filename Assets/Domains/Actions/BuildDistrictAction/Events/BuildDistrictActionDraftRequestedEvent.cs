namespace Domains.Actions.BuildDistrictAction.Events
{
    // One-frame pulse: "a district-build draft is requested" — raised by the DistrictBuild UI when the build
    // window opens (Presentation → Actions; the UI raise-site is a later slice). BuildDistrictDraftSpawnSystem
    // reconciles to exactly one draft BuildDistrictAction. Payload-less; cleared by EventCleanupSystem.
    public struct BuildDistrictActionDraftRequestedEvent
    {
    }
}
