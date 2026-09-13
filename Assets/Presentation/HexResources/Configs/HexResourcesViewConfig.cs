using System;
using Presentation.HexResources.Data;
using UnityEngine;
using EcsExtensions;

namespace Presentation.HexResources.Configs
{
    [CreateAssetMenu(fileName = "HexResourcesViewConfig", menuName = "FantasyMayor/HexResourcesView/HexResourcesViewConfig")]
    public sealed class HexResourcesViewConfig : ScriptableObject, IValidatableConfig
    {
        [SerializeField] private HexResourcesViewConfigEntry[] _resources = Array.Empty<HexResourcesViewConfigEntry>();

        public HexResourcesViewConfigEntry[] Resources => _resources ?? Array.Empty<HexResourcesViewConfigEntry>();

        public void Validate()
        {
            foreach (var resource in Resources)
                if (resource.Prefab == null)
                    throw new Exception($"Game resources view config contains null prefab for resource type '{resource.Type}'.");
        }
    }
}
