using UnityEngine;

namespace Domains.Economy.DistrictBuild.Configs
{
    [CreateAssetMenu(fileName = "DistrictsBuildConfig", menuName = "FantasyMayor/Districts/DistrictBuildsConfig")]
    public class DistrictBuildsConfig : ScriptableObject
    {
        [SerializeField] public DistrictBuildConfig[] _districts;
        public DistrictBuildConfig[] Districts => _districts;
    }
}
