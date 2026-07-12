using UnityEngine;

namespace Domains.Economy.DistrictOpenCondition.Configs
{
    // Container catalogue: the full list of district-open conditions, authored as references to concrete
    // DistrictOpenConditionConfig subclass assets. Loaded once at ConfigLoadStep; the spawn orchestrator
    // materializes each entry into its own entity.
    [CreateAssetMenu(
        fileName = "DistrictOpenConditionsConfig",
        menuName = "FantasyMayor/Districts/DistrictOpenConditionsConfig")]
    public sealed class DistrictOpenConditionsConfig : ScriptableObject
    {
        [SerializeField] private DistrictOpenConditionConfig[] _conditions;
        public DistrictOpenConditionConfig[] Conditions => _conditions;
    }
}
