using UnityEngine;

namespace Presentation.Terrain.Data
{
    [CreateAssetMenu(fileName = "WindErosionConfig", menuName = "FantasyMayor/Terrain/Wind Erosion Config")]
    public class WindErosionConfig : ScriptableObject
    {
        [SerializeField] private bool _enableWindErosion;
        [Range(0f, 1f)]
        [SerializeField] private float _windStrength = 0.35f;
        [Range(0f, 1f)]
        [SerializeField] private float _windTransportRate = 0.3f;
        [Min(1)]
        [SerializeField] private int _windErosionIterations = 8;
        [Min(0f)]
        [SerializeField] private float _windMaxTransportPerIteration = 0.05f;

        public bool EnableWindErosion => _enableWindErosion;
        public float WindStrength => Mathf.Clamp01(_windStrength);
        public float WindTransportRate => Mathf.Clamp01(_windTransportRate);
        public int WindErosionIterations => Mathf.Max(1, _windErosionIterations);
        public float WindMaxTransportPerIteration => Mathf.Max(0f, _windMaxTransportPerIteration);
    }
}
