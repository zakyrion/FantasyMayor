using UnityEngine;

namespace Modules.HexResources.Configs
{
    [CreateAssetMenu(fileName = "ForestResourceConfig", menuName = "FantasyMayor/HexResources/ForestResourceConfig")]
    internal sealed class ForestResourceConfig : ResourceConfig
    {
        [SerializeField] private Vector2Int _zoneCount;
        [SerializeField] private Vector2Int _zoneSize;

        public Vector2Int ZoneCount => _zoneCount;
        public Vector2Int ZoneSize => _zoneSize;
    }
}
