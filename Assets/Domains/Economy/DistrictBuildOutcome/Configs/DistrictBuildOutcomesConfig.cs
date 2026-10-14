using UnityEngine;
using Domains.Economy.DistrictBuildOutcome.Configs;
using System;
using EcsExtensions;

namespace Domains.Economy.DistrictBuildOutcome.Configs{
    // Container catalogue: the full list of build-district outcomes, authored as references to concrete
    // BuildDistrictOutcomeConfig subclass assets. Loaded once at AppState.ConfigLoading; the spawn orchestrator
    // materializes each entry into its own entity (one outcome row per district type).
    [CreateAssetMenu(
        fileName = "BuildDistrictOutcomesConfig",
        menuName = "FantasyMayor/Districts/BuildDistrictOutcomesConfig")]
    public sealed class DistrictBuildOutcomesConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField] private DistrictBuildOutcomeConfig[] _outcomes;
        public DistrictBuildOutcomeConfig[] Outcomes => _outcomes;

        public void Validate()
        {
            if (Outcomes == null)
                throw new InvalidOperationException("BuildDistrictOutcomesConfig: Outcomes array is null.");

            for (var index = 0; index < Outcomes.Length; index++)
            {
                if (Outcomes[index] == null)
                    throw new InvalidOperationException(
                        $"BuildDistrictOutcomesConfig: outcome entry at index {index} is null.");
            }
        }
    }
}
