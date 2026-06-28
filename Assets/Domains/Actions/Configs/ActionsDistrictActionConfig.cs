using Domains.Economy.District.Data;
using UnityEngine;

namespace Domains.Actions.Configs
{
    [CreateAssetMenu(fileName = "ActionsDistrictConfig", menuName = "FantasyMayor/Actions/ActionsDistrictConfig")]
    public class ActionsDistrictActionConfig : ScriptableObject
    {
        [SerializeField]
        private DistrictType _districtType;
    }
}
