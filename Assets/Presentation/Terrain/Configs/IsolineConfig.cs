using Presentation.Terrain.Data;
using UnityEngine;

namespace Presentation.Terrain.Configs
{
    /// <summary>
    ///     Settings of a single field-based isoline: traversal side, depth band parameters, and natural-curve
    ///     shape settings. Abstract because configs are stored by type — the inner and outer lines are
    ///     <see cref="OuterIsolineConfig" /> and <see cref="InnerIsolineConfig" />.
    /// </summary>
    public abstract class IsolineConfig : ScriptableObject
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
    }
}
