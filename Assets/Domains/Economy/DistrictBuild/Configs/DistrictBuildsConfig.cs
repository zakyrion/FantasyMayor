using UnityEngine;
using System;
using Domains.Kernel.Data;
using EcsExtensions;

namespace Domains.Economy.DistrictBuild.Configs
{
    [CreateAssetMenu(fileName = "DistrictsBuildConfig", menuName = "FantasyMayor/Districts/DistrictBuildsConfig")]
    public class DistrictBuildsConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField] public DistrictBuildConfig[] _districts;
        public DistrictBuildConfig[] Districts => _districts;

        public void Validate()
        {
            if (Districts == null)
                throw new InvalidOperationException("DistrictsBuildConfig: Districts array is null.");

            for (var index = 0; index < Districts.Length; index++)
            {
                var district = Districts[index];
                if (district == null)
                    throw new InvalidOperationException(
                        $"DistrictsBuildConfig: district entry at index {index} is null.");

                if (district.AllowedOwners == ActorType.Unknown)
                    throw new InvalidOperationException(
                        $"DistrictsBuildConfig: district '{district.DistrictType}' at index {index} " +
                        "has no allowed owners (AllowedOwners is Unknown).");
            }
        }
    }
}
