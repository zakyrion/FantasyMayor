using System;
using Domains.Actions.Configs;
using Domains.Economy.District.Configs;
using Domains.Economy.District.Data;
using Domains.Economy.Resource.Data;
using Domains.Map.Hex.Data;
using Domains.Map.HexResources.Data;

namespace Presentation.UI.DistrictBuild.Views.SubViews
{
    /// <summary>
    ///     Read-only data surface the panel sub-views pull from. Implemented by <c>DistrictBuildUIView</c>, which
    ///     owns the pushed catalogues + payer pools. It carries NO interaction state — the active payer is passed
    ///     into <see cref="AmountOf" /> explicitly (payer state lives in <c>DistrictPayerSubView</c>), so the
    ///     read-model stays a pure projection of the pushed data.
    /// </summary>
    internal interface IDistrictBuildData
    {
        HexType SelectedHexType { get; }
        int MayorAp { get; }

        /// <summary>The selected hex's HexResources (≤ one per type). Empty = a hex with no resources.</summary>
        ReadOnlySpan<HexResourceType> HexResources { get; }

        /// <summary>Stockpile of the given payer (Мер / Місто) for a resource type.</summary>
        int AmountOf(Payer payer, ResourceType type);

        /// <summary>Authoritative buildability gate (terrain + resource) from the Economy config.</summary>
        bool IsAvailable(DistrictBuildingConfig district);

        /// <summary>The Actions cost entry for a district (AP + prices); throws if the catalogue lacks it.</summary>
        ActionsDistrictBuildConfig CostFor(DistrictType type);
    }
}
