using System;
using Modules.HexResources.Data;
using UnityEngine;

namespace Modules.HexResources.Configs
{
    [CreateAssetMenu(fileName = "HexResourcesConfig", menuName = "FantasyMayor/HexResources/HexResourcesConfig")]
    internal sealed class HexResourcesConfig : ScriptableObject
    {
        [SerializeField] private HexResourcesConfigEntry[] _resources = Array.Empty<HexResourcesConfigEntry>();

        public HexResourcesConfigEntry[] Resources => _resources ?? Array.Empty<HexResourcesConfigEntry>();
    }
}
