using Domains.Economy.District.Data;
using Domains.Kernel.Data;
using Modules.AxialSystem;

namespace Domains.Actions.BuildDistrictAction.Events
{
    /// <summary>
    ///     One-frame event: the player confirmed «Збудувати» in the district-build overlay. The view raises the
    ///     local C# Confirmed event; <c>DistrictBuildUISystem</c> handles it, reads the selection, and raises this on
    ///     its own entity alongside <c>EventTag</c> (build and close are handled separately — confirm both builds and
    ///     hides). CARRIES the hex, district, and chosen payer: on it the reactive <c>BuildDistrictActionSystem</c>
    ///     creates the committed build entity directly (there is no draft). The payload crosses the asmdef boundary
    ///     because the Actions assembly cannot read the Presentation selection state (<c>HexSelectedComponent</c> /
    ///     <c>DistrictBuildSelectionComponent</c> / the price view's payer) — that would invert the existing
    ///     Presentation → Actions dependency. <c>Payer</c> is captured now but not yet spent (resource spend is R2).
    ///     Cleared by <c>EventCleanupSystem</c>.
    /// </summary>
    public struct DistrictBuildConfirmedEvent
    {
        public HexCoord Coords;
        public DistrictType Type;
        public ActorType Payer;
    }
}
