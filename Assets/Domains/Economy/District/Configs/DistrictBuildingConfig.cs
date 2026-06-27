using System;
using System.Collections.Generic;
using Domains.Economy.District.Data;
using Domains.Economy.Resource.Components;
using Domains.Map.Hex.Data;
using Domains.Map.HexResources.Data;
using UnityEngine;

namespace Domains.Economy.District.Configs
{
    [CreateAssetMenu(fileName = "DistrictBuildingConfig", menuName = "FantasyMayor/Districts/DistrictBuildingConfig")]
    public class DistrictBuildingConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictType _districtType;
        [SerializeField]
        private List<ResourceComponent> _districtPrices;
        [SerializeField]
        private List<HexType> _hexTypesRequirement;
        [SerializeField]
        private List<HexType> _impossibleToBuildTypes;
        [SerializeField]
        private ResourceType _requiredResourceType;
        [SerializeField]
        private bool _needEmptyHexResourcesToBuild;
        [SerializeField]
        private int _actionPointsRequired;

        public int ActionPointsRequired => _actionPointsRequired;
        public List<ResourceComponent> DistrictPrices => _districtPrices;
        public DistrictType DistrictType => _districtType;
        public List<HexType> HexTypesRequirement => _hexTypesRequirement;
        public List<HexType> ImpossibleToBuildTypes => _impossibleToBuildTypes;
        public ResourceType RequiredResourceType => _requiredResourceType;
        public bool NeedEmptyHexResourcesToBuild => _needEmptyHexResourcesToBuild;

        // Single availability gate. Terrain first (impossible list, then requirement list), then the resource gate.
        // The two resource modes are mutually exclusive: NeedEmptyHexResourcesToBuild demands a hex with NO
        // resources; otherwise the hex must carry the one RequiredResourceType. hexResources is the selected hex's
        // HexResources set (empty span = a hex with no resources).
        public bool CanBuildOn(HexType hexType, ReadOnlySpan<ResourceType> hexResources)
        {
            if (_impossibleToBuildTypes.Contains(hexType))
                return false;

            if (_hexTypesRequirement.Count > 0 && !_hexTypesRequirement.Contains(hexType))
                return false;

            if (_needEmptyHexResourcesToBuild)
                return hexResources.Length == 0;

            foreach (var resource in hexResources)
                if (resource == _requiredResourceType)
                    return true;

            return false;
        }
    }
}
