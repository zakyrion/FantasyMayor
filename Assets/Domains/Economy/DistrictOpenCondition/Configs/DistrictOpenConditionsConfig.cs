using UnityEngine;
using System;
using EcsExtensions;

namespace Domains.Economy.DistrictOpenCondition.Configs
{
    // Container catalogue: the full list of district-open conditions, authored as references to concrete
    // DistrictOpenConditionConfig subclass assets. Loaded once at AppState.ConfigLoading; the spawn orchestrator
    // materializes each entry into its own entity.
    [CreateAssetMenu(
        fileName = "DistrictOpenConditionsConfig",
        menuName = "FantasyMayor/Districts/DistrictOpenConditionsConfig")]
    public sealed class DistrictOpenConditionsConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField] private DistrictOpenConditionConfig[] _conditions;
        public DistrictOpenConditionConfig[] Conditions => _conditions;

        public void Validate()
        {
            if (Conditions == null)
                throw new InvalidOperationException("DistrictOpenConditionsConfig: Conditions array is null.");

            for (var index = 0; index < Conditions.Length; index++)
            {
                if (Conditions[index] == null)
                    throw new InvalidOperationException(
                        $"DistrictOpenConditionsConfig: condition entry at index {index} is null.");
            }
        }
    }
}
