using System;
using Modules.HexResourcesView.Data;
using UnityEngine;

namespace Modules.HexResourcesView.Configs
{
    [CreateAssetMenu(fileName = "HexResourcesViewConfig", menuName = "FantasyMayor/HexResourcesView/HexResourcesViewConfig")]
    internal sealed class HexResourcesViewConfig : ScriptableObject
    {
        [SerializeField] private HexResourcesViewConfigEntry[] _resources = Array.Empty<HexResourcesViewConfigEntry>();

        public HexResourcesViewConfigEntry[] Resources => _resources ?? Array.Empty<HexResourcesViewConfigEntry>();
    }
}
