using UnityEngine;

namespace Modules.TerrainGenerator.Configs
{
    /// <summary>Controls the placement and size of a lake on the terrain.</summary>
    [CreateAssetMenu(fileName = "LakeConfig", menuName = "FantasyMayor/Terrain/Water/LakeConfig")]
    public class LakeConfig : ScriptableObject
    {
        [Header("Placement")]
        [Tooltip("Minimum distance in tiles from the map edge within which the lake centre cannot spawn.")]
        [SerializeField] [Min(0)] private int _edgeMarginTiles = 2;

        [Header("Size")]
        [Tooltip("Lake area as a fraction of the total tile count (0–1).")]
        [SerializeField] [Range(0f, 1f)] private float _sizeFraction = 0.05f;

        /// <summary>Minimum distance in tiles from the map edge within which the lake centre cannot spawn.</summary>
        public int EdgeMarginTiles => _edgeMarginTiles;

        /// <summary>Lake area as a fraction of the total tile count (0–1).</summary>
        public float SizeFraction => _sizeFraction;
    }
}
