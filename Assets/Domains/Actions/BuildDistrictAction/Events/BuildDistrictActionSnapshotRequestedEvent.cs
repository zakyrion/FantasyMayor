using Domains.Economy.District.Data;
using Domains.Map.Hex.Components;

namespace Domains.Actions.BuildDistrictAction.Events
{
    // One-frame pulse carrying the player's district-build selection into the Actions domain: which DistrictType,
    // on which hex (as the hex entity's HexIdComponent FK). Raised by the DistrictBuild UI as the player configures
    // the build (Presentation → Actions; the UI raise-site is a later slice). BuildDistrictActionSnapshotSystem
    // reads the payload and writes it — plus the joined cost — onto the draft BuildDistrictAction. The small
    // identifying payload is deliberate: the choice originates in the UI (Presentation), which Actions does not
    // reference, so it arrives as a payload rather than being reconciled from world state (PATTERN_EVENT
    // tolerated-payload case). Cleared by EventCleanupSystem.
    public struct BuildDistrictActionSnapshotRequestedEvent
    {
        public DistrictType DistrictType;
        public HexIdComponent TargetHex;
    }
}
