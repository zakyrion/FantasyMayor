using UnityEngine;

namespace Domains.Map.HexResources.Configs
{
    [CreateAssetMenu(fileName = "ClayResourceConfig", menuName = "FantasyMayor/HexResources/ClayResourceConfig")]
    internal sealed class ClayResourceConfig : ResourceConfig
    {
        [SerializeField] private Vector2Int _count;
        [SerializeField] private int _distanceToWater;

        public Vector2Int Count => _count;
        public int DistanceToWater => _distanceToWater;
    }
}
