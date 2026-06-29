using System;
using Domains.Actions.Configs;
using Domains.Economy.District.Configs;
using Domains.Economy.District.Data;
using Domains.Economy.Resource.Data;
using Domains.Map.Hex.Data;
using Domains.Map.HexResources.Data;

namespace Presentation.UI.DistrictBuild.Views.Binders
{
    /// <summary>
    ///     Read-only data surface the section binders pull from. Implemented by <c>DistrictBuildUIView</c>, which
    ///     owns the pushed catalogues + payer pools and the current selection/payer state. Binders render; the
    ///     view stays the single source of state and of the authoritative gate (<see cref="IsAvailable" />).
    /// </summary>
    internal interface IDistrictBuildData
    {
        HexType SelectedHexType { get; }
        int MayorAp { get; }

        /// <summary>The selected hex's HexResources (≤ one per type). Empty = a hex with no resources.</summary>
        ReadOnlySpan<HexResourceType> HexResources { get; }

        /// <summary>Stockpile of the currently active payer (Мер / Місто) for the given resource type.</summary>
        int AmountOfActivePayer(ResourceType type);

        /// <summary>Authoritative buildability gate (terrain + resource) from the Economy config.</summary>
        bool IsAvailable(DistrictBuildingConfig district);

        /// <summary>The Actions cost entry for a district (AP + prices); throws if the catalogue lacks it.</summary>
        ActionsDistrictBuildConfig CostFor(DistrictType type);
    }
}
