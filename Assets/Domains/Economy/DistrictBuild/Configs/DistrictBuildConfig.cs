using System;
using System.Collections.Generic;
using Domains.Economy.District.Data;
using Domains.Kernel.Data;
using Domains.Map.Hex.Data;
using Domains.Map.HexResources.Data;
using UnityEngine;

namespace Domains.Economy.DistrictBuild.Configs
{
    [CreateAssetMenu(fileName = "DistrictBuildConfig", menuName = "FantasyMayor/Districts/DistrictBuildConfig")]
    public class DistrictBuildConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictType _districtType;
        [SerializeField]
        private ActorType _allowedOwners;
        [SerializeField]
        private List<HexType> _impossibleToBuildTypes;
        [SerializeField]
        private HexResourceType _requiredHexResourceType;
        [SerializeField]
        private bool _needEmptyHexResourcesToBuild;

        public DistrictType DistrictType => _districtType;
        public ActorType AllowedOwners => _allowedOwners;
        public List<HexType> ImpossibleToBuildTypes => _impossibleToBuildTypes;
        public HexResourceType RequiredHexResourceType => _requiredHexResourceType;
        public bool NeedEmptyHexResourcesToBuild => _needEmptyHexResourcesToBuild;

        // Single availability gate = terrain gate AND resource gate. Decomposed into per-dimension predicates
        // so the build UI can surface each requirement on its own line from the SAME logic (no divergence).
        // hexResources is the selected hex's HexResources set (empty span = a hex with no resources).
        public bool CanBuildOn(HexType hexType, ReadOnlySpan<HexResourceType> hexResources) =>
            IsTerrainAllowed(hexType) && IsResourceSatisfied(hexResources);

        // Terrain gate: an explicit blacklist hit forbids; a non-empty whitelist that omits the type forbids.
        private bool IsTerrainAllowed(HexType hexType) => !_impossibleToBuildTypes.Contains(hexType);

        // Resource gate, two mutually exclusive modes: NeedEmptyHexResourcesToBuild demands a hex with NO
        // resources; otherwise the hex must carry the one RequiredHexResourceType. An unset (Unknown) required
        // resource in non-empty mode is an authoring error — fail loud, not a silent "forbidden on every hex".
        private bool IsResourceSatisfied(ReadOnlySpan<HexResourceType> hexResources)
        {
            if (_needEmptyHexResourcesToBuild)
                return hexResources.Length == 0;

            if (_requiredHexResourceType == HexResourceType.Unknown)
                throw new InvalidOperationException(
                    $"DistrictBuildingConfig ({_districtType}): RequiredHexResourceType is Unknown while "
                    + "NeedEmptyHexResourcesToBuild is false — set a required resource or enable the empty-hex mode.");

            foreach (var resource in hexResources)
                if (resource == _requiredHexResourceType)
                    return true;

            return false;
        }
    }
}
