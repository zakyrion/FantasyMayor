using UnityEngine;

namespace Domains.Map.Generation.Configs
{
    /// <summary>Controls how much of the terrain is covered by sea.</summary>
    [CreateAssetMenu(fileName = "SeaConfig", menuName = "FantasyMayor/Terrain/Water/SeaConfig")]
    public class SeaConfig : ScriptableObject
    {
        [Header("Size")]
        [Tooltip("Sea area as a fraction of the total tile count (0–1).")]
        [SerializeField] [Range(0f, 1f)] private float _sizeFraction = 0.3f;

        /// <summary>Sea area as a fraction of the total tile count (0–1).</summary>
        public float SizeFraction => _sizeFraction;
    }
}
