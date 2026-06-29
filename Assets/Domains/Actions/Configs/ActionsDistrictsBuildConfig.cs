using UnityEngine;

namespace Domains.Actions.Configs
{
    [CreateAssetMenu(fileName = "ActionsDistrictsBuildConfig", menuName = "FantasyMayor/Actions/ActionsDistrictsBuildConfig")]
    public class ActionsDistrictsBuildConfig : ScriptableObject
    {
        [SerializeField]
        private ActionsDistrictBuildConfig[] _districts;

        public ActionsDistrictBuildConfig[] Districts => _districts;
    }
}
