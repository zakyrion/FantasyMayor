namespace Domains.Actions.BuildDistrictAction.Tags
{
    // Discriminator for the committed slice of the build-district-action table (PK: ActionIdComponent, base
    // discriminator: BuildDistrictActionTag). Present ⇒ AP + resources have been spent and the build is counting
    // down (ActionTurnLeftComponent). Absent while BuildDistrictActionTag is present ⇒ still an uncommitted draft.
    public struct BuildDistrictActionCommittedTag
    {
    }
}
