using Domains.Economy.District.Data;
using UnityEngine;

namespace Domains.Actions.BuildDistrictAction.Configs{
    [CreateAssetMenu(fileName = "DistrictActionConfig", menuName = "FantasyMayor/Actions/DistrictActionConfig")]
    public class BuildDistrictActionConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictType _districtType;
    }
}
