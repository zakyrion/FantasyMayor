using UnityEngine;

namespace Domains.Economy.District.Configs
{
    [CreateAssetMenu(fileName = "DistrictsBuildConfig", menuName = "FantasyMayor/Districts/DistrictsBuildConfig")]
    public class DistrictsBuildConfig : ScriptableObject
    {
        [SerializeField] public DistrictBuildingConfig[] _districts;
        public DistrictBuildingConfig[] Districts => _districts;
    }
}
