using UnityEngine;

namespace Presentation.Districts.Configs
{
    [CreateAssetMenu(fileName = "DistrictViewsConfig", menuName = "FantasyMayor/Presentation/DistrictViewsConfig")]
    public class DistrictViewsConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictViewConfig[] _views;

        public DistrictViewConfig[] Views => _views;
    }
}
