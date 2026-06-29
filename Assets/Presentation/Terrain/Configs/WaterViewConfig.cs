using UnityEngine;

namespace Presentation.Terrain.Configs
{
    /// <summary>
    ///     All stylistic and simulation parameters for the animated water surface.
    ///     Consumed by <see cref="Systems.WaterViewSubSystem" /> at terrain generation time.
    /// </summary>
    [CreateAssetMenu(fileName = "WaterViewConfig", menuName = "FantasyMayor/Water View Config")]
    public class WaterViewConfig : ScriptableObject
    {
        [Header("Geometry")]
        [Tooltip("World-unit offset above the terrain water depression floor. Keep slightly positive to avoid z-fighting.")]
        [Range(0f, 0.5f)]
        public float waterYOffset = 0.05f;

        [Tooltip("Number of triangle subdivisions per hex face. 1 = cheapest, 2 = smoother shoreline.")]
        [Range(1, 4)]
        public int subdivisions = 1;

        [Header("Colors")]
        [Tooltip("Water color at the shore / shallow areas.")]
        public Color shallowColor = new(0.35f, 0.72f, 0.85f, 0.65f);

        [Tooltip("Water color in deep zones.")]
        public Color deepColor = new(0.10f, 0.28f, 0.58f, 0.92f);

        [Header("Normal Map")]
        [Tooltip("Wave normal map texture. Assign a tileable water-normal texture.")]
        public Texture2D waveNormalMap;

        [Header("Wave Animation")]
        [Tooltip("Peak vertical displacement of wave geometry in world units.")]
        [Range(0f, 0.3f)]
        public float waveAmplitude = 0.04f;

        [Tooltip("Spatial frequency of wave geometry. Higher = tighter wave crests.")]
        [Range(0.5f, 5f)]
        public float waveFrequency = 1.5f;

        [Tooltip("Overall scrolling speed multiplier for both normal-map layers.")]
        [Range(0f, 5f)]
        public float waveSpeed = 0.4f;

        [Tooltip("UV tiling for normal-map layer 1 (primary wave direction).")]
        public Vector2 waveScale = new(2f, 2f);

        [Tooltip("UV tiling for normal-map layer 2 (cross-wave). Different scale adds organic look.")]
        public Vector2 waveScale2 = new(1.5f, 1.5f);

        [Header("Flow")]
        [Tooltip("Dominant current direction in local XZ. Automatically normalised at runtime. " +
                 "(1,0) = flow along +X, (0,1) = flow along +Z.")]
        public Vector2 flowDirection = new(1f, 0f);

        [Tooltip("Extra scroll speed along FlowDirection, layered on top of WaveSpeed.")]
        [Range(0f, 3f)]
        public float flowSpeed = 0.2f;

        [Header("Shore Foam")]
        [Tooltip("Peak foam brightness. 0 = no foam, 1 = full white foam at shore edge.")]
        [Range(0f, 1f)]
        public float foamStrength = 0.75f;

        [Tooltip("Depth-buffer distance (world units) over which foam fades to zero. " +
                 "Requires URP Depth Texture enabled in the renderer asset.")]
        [Range(0.05f, 5f)]
        public float foamWidth = 0.4f;

        [Header("Surface Shading")]
        [Tooltip("Fresnel exponent. Higher values produce a tighter specular rim.")]
        [Range(1f, 12f)]
        public float fresnelPower = 4f;

        [Tooltip("Base surface alpha in deep water. 1 = fully opaque, 0 = invisible.")]
        [Range(0f, 1f)]
        public float transparency = 0.82f;
    }
}
