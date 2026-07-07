using Domains.Economy.District.Data;
using Modules.AxialSystem;

namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     One-frame event: the district-build overlay opened, so the draft build entity must be spawned. Raised on
    ///     its own entity alongside <c>EventTag</c> by <c>DistrictBuildUISystem</c> (Presentation) and consumed by
    ///     <c>BuildDistrictTemplateSpawnSystem</c>. Unlike the payload-less pulses in this domain it CARRIES the hex
    ///     and district: the Actions assembly cannot read the Presentation selection state
    ///     (<c>HexSelectedComponent</c> / <c>DistrictBuildSelectionComponent</c>) — that would invert the existing
    ///     Presentation → Actions assembly dependency — so the choice is handed over as payload. Cleared by
    ///     <c>EventCleanupSystem</c>.
    /// </summary>
    public struct DistrictBuildStartedEvent
    {
        public HexCoord Coords;
        public DistrictType Type;
    }
}
