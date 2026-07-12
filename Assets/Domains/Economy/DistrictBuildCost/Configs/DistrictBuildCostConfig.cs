using System.Collections.Generic;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictBuildCost.Data;
using UnityEngine;

namespace Domains.Economy.DistrictBuildCost.Configs{
    [CreateAssetMenu(fileName = "DistrictBuildCostConfig", menuName = "FantasyMayor/Actions/DistrictBuildCostConfig")]
    public class DistrictBuildCostConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictType _districtType;
        [SerializeField]
        private List<ResourceCost> _districtPrices;
        [SerializeField]
        private int _apPrice;
        [SerializeField][Range(0,100)]
        private int _turnsToBuild = 1;

        public DistrictType DistrictType => _districtType;
        public List<ResourceCost> DistrictPrices => _districtPrices;
        public int ApPrice => _apPrice;
        public int TurnsToBuild => _turnsToBuild;
    }
}
