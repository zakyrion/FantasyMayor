using Domains.Economy.District.Data;
using UnityEngine;

namespace Presentation.Districts.Configs
{
    [CreateAssetMenu(fileName = "DistrictBuildProgressViewConfig", menuName = "FantasyMayor/Presentation/DistrictBuildProgressViewConfig")]
    public class DistrictBuildProgressViewConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictType _districtType;
        [SerializeField]
        private GameObject _prefab;

        public DistrictType DistrictType => _districtType;
        public GameObject Prefab => _prefab;
    }
}
