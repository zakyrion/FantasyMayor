using Domains.Actors.Data;

namespace Domains.Actions.BuildDistrictAction.Events
{
    // One-frame pulse: the player confirmed the drafted district build and chose who pays for it. Payload = the
    // paying owner (ActorType + its id): resources come from this owner, while the AP cost is always paid by the
    // mayor. Raised by the DistrictBuild UI (Presentation → Actions; the raise-site is a later slice).
    // BuildDistrictActionCommitSystem consumes it. Cleared by EventCleanupSystem.
    public struct BuildDistrictActionCommitRequestedEvent
    {
        public ActorType OwnerType;
        public int OwnerId;
    }
}
