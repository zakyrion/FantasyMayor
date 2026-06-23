using UnityEngine;

namespace Domains.Map.HexResources.Configs
{
    [CreateAssetMenu(fileName = "FishResourceConfig", menuName = "FantasyMayor/HexResources/FishResourceConfig")]
    internal sealed class FishResourceConfig : ResourceConfig
    {
        [SerializeField] private Vector2Int _count;
        [SerializeField] private int _distanceToShore;

        public Vector2Int Count => _count;
        public int DistanceToShore => _distanceToShore;
    }
}
