using UnityEngine;

namespace Domains.Map.Generation.Configs
{
    /// <summary>Controls how far river endpoints are kept away from map corners.</summary>
    [CreateAssetMenu(fileName = "RiverConfig", menuName = "FantasyMayor/Terrain/Water/RiverConfig")]
    public class RiverConfig : ScriptableObject
    {
        [Header("Placement")]
        [Tooltip("How many edge tiles to skip from each corner when selecting river endpoints.")]
        [SerializeField] [Min(0)] private int _cornerOffsetTiles = 1;

        /// <summary>How many edge tiles to skip from each corner when selecting river endpoints.</summary>
        public int CornerOffsetTiles => _cornerOffsetTiles;
    }
}
