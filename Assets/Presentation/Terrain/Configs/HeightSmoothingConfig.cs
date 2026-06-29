using UnityEngine;

namespace Presentation.Terrain.Data
{
    [CreateAssetMenu(fileName = "HeightSmoothingConfig", menuName = "FantasyMayor/Terrain/Height Smoothing Config")]
    public class HeightSmoothingConfig : ScriptableObject
    {
        [SerializeField] private bool _enableHeightBlur = true;
        [Min(0)]
        [SerializeField] private int _heightBlurRadius = 1;
        [Min(1)]
        [SerializeField] private int _heightBlurIterations = 1;

        public bool EnableHeightBlur => _enableHeightBlur;
        public int HeightBlurRadius => Mathf.Max(0, _heightBlurRadius);
        public int HeightBlurIterations => Mathf.Max(1, _heightBlurIterations);
    }
}
