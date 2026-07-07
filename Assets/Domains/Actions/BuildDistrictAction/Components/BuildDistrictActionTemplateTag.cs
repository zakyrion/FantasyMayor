namespace Domains.Actions.BuildDistrictAction.Components
{
    // Marks the draft build entity while the district-build overlay is open: spawned on the
    // DistrictBuildStartedEvent pulse, discarded on DistrictBuildCancelledEvent, and swapped for
    // BuildDistrictActionTag when the player confirms. "Template" = drafted, not yet committed.
    public struct BuildDistrictActionTemplateTag
    {
    }
}
