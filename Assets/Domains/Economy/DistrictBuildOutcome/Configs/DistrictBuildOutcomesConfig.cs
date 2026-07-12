using UnityEngine;
using Domains.Economy.DistrictBuildOutcome.Configs;

namespace Domains.Economy.DistrictBuildOutcome.Configs{
    // Container catalogue: the full list of build-district outcomes, authored as references to concrete
    // BuildDistrictOutcomeConfig subclass assets. Loaded once at ConfigLoadStep; the spawn orchestrator
    // materializes each entry into its own entity (one outcome row per district type).
    [CreateAssetMenu(
        fileName = "BuildDistrictOutcomesConfig",
        menuName = "FantasyMayor/Districts/BuildDistrictOutcomesConfig")]
    public sealed class DistrictBuildOutcomesConfig : ScriptableObject
    {
        [SerializeField] private DistrictBuildOutcomeConfig[] _outcomes;
        public DistrictBuildOutcomeConfig[] Outcomes => _outcomes;
    }
}
