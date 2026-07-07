using Domains.Economy.District.Data;
using UnityEngine;

namespace Presentation.Districts.Configs
{
    [CreateAssetMenu(fileName = "DistrictViewConfig", menuName = "FantasyMayor/Presentation/DistrictViewConfig")]
    public class DistrictViewConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictType _districtType;
        [SerializeField]
        private GameObject _prefab;

        public DistrictType DistrictType => _districtType;
        public GameObject Prefab => _prefab;
    }
}
