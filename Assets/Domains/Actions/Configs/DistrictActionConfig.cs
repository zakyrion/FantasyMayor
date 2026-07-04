using Domains.Economy.District.Data;
using UnityEngine;

namespace Domains.Actions.Configs
{
    [CreateAssetMenu(fileName = "DistrictActionConfig", menuName = "FantasyMayor/Actions/DistrictActionConfig")]
    public class DistrictActionConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictType _districtType;
    }
}
