using System;
using Presentation.Resources.Data;
using UnityEngine;

namespace Presentation.Resources.Configs
{
    [CreateAssetMenu(fileName = "HexResourcesViewConfig", menuName = "FantasyMayor/HexResourcesView/HexResourcesViewConfig")]
    public sealed class HexResourcesViewConfig : ScriptableObject
    {
        [SerializeField] private HexResourcesViewConfigEntry[] _resources = Array.Empty<HexResourcesViewConfigEntry>();

        public HexResourcesViewConfigEntry[] Resources => _resources ?? Array.Empty<HexResourcesViewConfigEntry>();
    }
}
