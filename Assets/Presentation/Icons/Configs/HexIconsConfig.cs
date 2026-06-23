using UnityEngine;

namespace Presentation.Icons.Configs
{
    [CreateAssetMenu(fileName = "HexIconsConfig", menuName = "FantasyMayor/HexIcons/HexIconsConfig")]
    internal sealed class HexIconsConfig : ScriptableObject
    {
        [SerializeField] private GameObject _prefab;
        public GameObject Prefab => _prefab;

        [SerializeField] private float _worldHeight;
        public float WorldHeight => _worldHeight;

        // Must mirror PanelSettings.PixelsPerUnit on the world-space prefab (WorldSpaceSettings asset).
        [SerializeField] private float _pixelsPerUnit = 100f;
        public float PixelsPerUnit => _pixelsPerUnit;

        // Per-hex icon square size in panel pixels (world size = IconSize / PixelsPerUnit).
        [SerializeField] private float _iconSize = 64f;
        public float IconSize => _iconSize;

        // World-space upward (Y) offset applied to the hex center before projection, so the icon "floats"
        // above the hex instead of sitting on it. Lifted in world space → foreshortens with perspective.
        [SerializeField] private float _worldYOffset = 1.5f;
        public float WorldYOffset => _worldYOffset;
    }
}
