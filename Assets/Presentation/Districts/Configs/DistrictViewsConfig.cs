using UnityEngine;
using System;
using EcsExtensions;

namespace Presentation.Districts.Configs
{
    [CreateAssetMenu(fileName = "DistrictViewsConfig", menuName = "FantasyMayor/Presentation/DistrictViewsConfig")]
    public class DistrictViewsConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField]
        private DistrictViewConfig[] _views;

        public DistrictViewConfig[] Views => _views;

        public void Validate()
        {
            if (Views == null)
                throw new InvalidOperationException("DistrictViewsConfig: Views array is null.");

            for (var index = 0; index < Views.Length; index++)
            {
                if (Views[index] == null)
                    throw new InvalidOperationException(
                        $"DistrictViewsConfig: view entry at index {index} is null.");

                if (Views[index].Prefab == null)
                    throw new InvalidOperationException(
                        $"DistrictViewsConfig: view entry at index {index} ('{Views[index].DistrictType}') has no prefab.");
            }
        }
    }
}
