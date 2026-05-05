using UnityEngine;

namespace Modules.TerrainView.Data
{
    /// <summary>
    ///     ScriptableObject asset that configures a single field-based isoline:
    ///     traversal side, depth band parameters, and natural-curve shape settings.
    ///     Create two instances — one for the inner line, one for the outer line.
    /// </summary>
    [CreateAssetMenu(fileName = "IsolineConfig", menuName = "FantasyMayor/Terrain/Isoline Config")]
    public class IsolineConfig : ScriptableObject
    {
        [Header("Traversal")]
        public TraversalSideData side;

        [Header("Depth")]
        public int depthCenter = 4;
        public int depthDeviation = 2;

        [Header("Natural Curve")]
        public int curveSeed = 1337;
        public float baseFrequency = 2.5f;
        public int octaves = 3;
        public float persistence = 0.55f;
        public float lacunarity = 2f;
        public int smoothingPasses = 1;

        [Header("Curve Preview")]
        public float previewLengthMultiplier = 3f;
        public int previewSamplesPerUnit = 48;
    }
}
