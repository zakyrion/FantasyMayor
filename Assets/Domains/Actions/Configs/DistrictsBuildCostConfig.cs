using UnityEngine;

namespace Domains.Actions.Configs
{
    [CreateAssetMenu(fileName = "DistrictsBuildCostConfig", menuName = "FantasyMayor/Actions/DistrictsBuildCostConfig")]
    public class DistrictsBuildCostConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictBuildCostConfig[] _districts;

        public DistrictBuildCostConfig[] Districts => _districts;
    }
}
