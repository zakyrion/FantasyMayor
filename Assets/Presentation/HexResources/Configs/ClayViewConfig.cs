using UnityEngine;

namespace Presentation.HexResources.Configs
{
    /// <summary>
    ///     Authoring config for the clay view: the depression footprint (ellipse → pear → noise) and the
    ///     clay color gradient. Clay is prefab-less, so it has its own config instead of an entry in
    ///     <see cref="HexResourcesViewConfig" /> (whose entries require a prefab).
    /// </summary>
    [CreateAssetMenu(fileName = "ClayViewConfig", menuName = "FantasyMayor/HexResourcesView/ClayViewConfig")]
    internal sealed class ClayViewConfig : ScriptableObject
    {
        [Header("Depression")]
        [Tooltip("Base footprint radius as a fraction of the hex CellSize.")]
        [SerializeField, Range(0f, 1f)] private float _depressionRadius = 0.6f;

        [Tooltip("How far the patch center sinks, in world units.")]
        [SerializeField] private float _depressionDepth = 0.15f;

        [Header("Footprint shape")]
        [Tooltip("Ellipse semi-axis ratio (x along world X, y along world Z). (1,1) = circle.")]
        [SerializeField] private Vector2 _footprintAspect = new(1f, 0.7f);

        [Tooltip("Pear asymmetry: 0 = symmetric ellipse, >0 bulges the contour toward +Z.")]
        [SerializeField] private float _pearFactor = 0.3f;

        [Tooltip("Strength of the organic edge noise (0 = clean ellipse/pear).")]
        [SerializeField, Range(0f, 1f)] private float _noiseAmplitude = 0.3f;

        [Tooltip("Angular frequency of the edge noise — higher is more jagged.")]
        [SerializeField] private float _noiseFrequency = 4f;

        [Header("Palette")]
        [Tooltip("Wet, dark clay color at the patch center.")]
        [SerializeField] private Color _clayCenterColor = new(0.369f, 0.180f, 0.110f, 1f); // #5E2E1C

        [Tooltip("Dry, light clay color at the patch rim.")]
        [SerializeField] private Color _clayRimColor = new(0.788f, 0.475f, 0.294f, 1f); // #C9794B

        public float DepressionRadius => _depressionRadius;
        public float DepressionDepth => _depressionDepth;
        public Vector2 FootprintAspect => _footprintAspect;
        public float PearFactor => _pearFactor;
        public float NoiseAmplitude => _noiseAmplitude;
        public float NoiseFrequency => _noiseFrequency;
        public Color ClayCenterColor => _clayCenterColor;
        public Color ClayRimColor => _clayRimColor;
    }
}
