using UnityEngine;

namespace Presentation.Districts.Configs
{
    [CreateAssetMenu(fileName = "DistrictBuildProgressViewsConfig", menuName = "FantasyMayor/Presentation/DistrictBuildProgressViewsConfig")]
    public class DistrictBuildProgressViewsConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictBuildProgressViewConfig[] _views;

        public DistrictBuildProgressViewConfig[] Views => _views;
    }
}
