using UnityEngine;

namespace Modules.HexIcons.Configs
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
    }
}
