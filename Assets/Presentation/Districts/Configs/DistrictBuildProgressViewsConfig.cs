using UnityEngine;
using System;
using EcsExtensions;

namespace Presentation.Districts.Configs
{
    [CreateAssetMenu(fileName = "DistrictBuildProgressViewsConfig", menuName = "FantasyMayor/Presentation/DistrictBuildProgressViewsConfig")]
    public class DistrictBuildProgressViewsConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField]
        private DistrictBuildProgressViewConfig[] _views;

        public DistrictBuildProgressViewConfig[] Views => _views;

        public void Validate()
        {
            if (Views == null)
                throw new InvalidOperationException("DistrictBuildProgressViewsConfig: Views array is null.");

            for (var index = 0; index < Views.Length; index++)
            {
                if (Views[index] == null)
                    throw new InvalidOperationException(
                        $"DistrictBuildProgressViewsConfig: view entry at index {index} is null.");

                if (Views[index].Prefab == null)
                    throw new InvalidOperationException(
                        $"DistrictBuildProgressViewsConfig: view entry at index {index} ('{Views[index].DistrictType}') has no prefab.");
            }
        }
    }
}
