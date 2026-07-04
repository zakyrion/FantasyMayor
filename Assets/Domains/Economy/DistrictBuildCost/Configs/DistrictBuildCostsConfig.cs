using UnityEngine;
using Domains.Economy.DistrictBuildCost.Configs;

namespace Domains.Economy.DistrictBuildCost.Configs{
    [CreateAssetMenu(fileName = "DistrictBuildCostsConfig", menuName = "FantasyMayor/Actions/DistrictBuildCostsConfig")]
    public class DistrictBuildCostsConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictBuildCostConfig[] _districts;

        public DistrictBuildCostConfig[] Districts => _districts;
    }
}
