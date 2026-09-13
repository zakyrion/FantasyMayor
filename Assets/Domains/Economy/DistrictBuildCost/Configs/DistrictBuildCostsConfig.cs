using UnityEngine;
using Domains.Economy.DistrictBuildCost.Configs;
using System;
using EcsExtensions;

namespace Domains.Economy.DistrictBuildCost.Configs{
    [CreateAssetMenu(fileName = "DistrictBuildCostsConfig", menuName = "FantasyMayor/Actions/DistrictBuildCostsConfig")]
    public class DistrictBuildCostsConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField]
        private DistrictBuildCostConfig[] _districts;

        public DistrictBuildCostConfig[] Districts => _districts;

        public void Validate()
        {
            if (Districts == null)
                throw new InvalidOperationException("DistrictsBuildCostConfig: Districts array is null.");

            for (var index = 0; index < Districts.Length; index++)
                if (Districts[index] == null)
                    throw new InvalidOperationException(
                        $"DistrictsBuildCostConfig: district entry at index {index} is null.");
        }
    }
}
