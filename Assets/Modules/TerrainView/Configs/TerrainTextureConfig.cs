using UnityEngine;

namespace Modules.TerrainView.Configs
{
    /// <summary>
    ///     Configuration for procedural terrain texture generation.
    ///     Defines per-terrain-type colors, brush parameters, and texture resolution.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainTextureConfig", menuName = "FantasyMayor/Terrain Texture Config")]
    public class TerrainTextureConfig : ScriptableObject
    {
        [Header("Texture")]
        [Tooltip("Output texture width and height in pixels.")]
        public int textureResolution = 2048;

        [Header("Brush")]
        [Tooltip("BFS depth for vertex color splatting. Higher = softer transitions.")]
        [Range(1, 10)]
        public int brushRadius = 3;

        [Tooltip("Box-blur radius applied to the final pixel buffer on a background thread. " +
                 "0 = no blur. Kernel size = (2 * radius + 1)².")]
        [Range(0, 20)]
        public int textureBlurRadius = 4;

        [Tooltip("Color weight at the brush center (BFS depth 0).")]
        [Range(0f, 2f)]
        public float centerWeight = 1f;

        [Tooltip("Color weight at the brush edge (BFS depth = brushRadius).")]
        [Range(0f, 1f)]
        public float edgeWeight = 0.3f;

        [Header("Slope")]
        [Tooltip("Normalized slope above which mountain/bedhill vertices use the steep color.")]
        [Range(0f, 2f)]
        public float slopeThreshold = 0.5f;

        [Header("Micro-Variation")]
        [Tooltip("Per-vertex hue jitter magnitude. 0 = no variation.")]
        [Range(0f, 0.2f)]
        public float hueJitterStrength = 0.05f;

        [Header("Colors")]
        public Color plainColor = new(0.55f, 0.78f, 0.45f, 1f);
        public Color mountainFlatColor = new(0.72f, 0.75f, 0.38f, 1f);
        public Color mountainSteepColor = new(0.58f, 0.55f, 0.50f, 1f);
        public Color bedhillColor = new(0.65f, 0.70f, 0.42f, 1f);
        public Color coastlineColor = new(0.82f, 0.76f, 0.55f, 1f);
        public Color waterColor = new(0.28f, 0.45f, 0.62f, 1f);
        public Color fallbackColor = new(0.4f, 0.5f, 0.35f, 1f);
    }
}
