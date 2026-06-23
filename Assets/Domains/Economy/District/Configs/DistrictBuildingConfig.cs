using System.Collections.Generic;
using Domains.Economy.District.Data;
using Domains.Economy.Resource.Components;
using Domains.Map.Hex.Data;
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
        private int _actionPointsRequired;

        public int ActionPointsRequired => _actionPointsRequired;
        public List<ResourceComponent> DistrictPrices => _districtPrices;
        public DistrictType DistrictType => _districtType;
        public List<HexType> HexTypesRequirement => _hexTypesRequirement;
        public List<HexType> ImpossibleToBuildTypes => _impossibleToBuildTypes;

        public bool CanBuildOn(HexType hexType)
        {
            if (_impossibleToBuildTypes.Contains(hexType))
                return false;

            return _hexTypesRequirement.Count == 0 || _hexTypesRequirement.Contains(hexType);

        }
    }
}
