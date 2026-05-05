using UnityEngine;

namespace Modules.TerrainView.Configs
{
    [CreateAssetMenu(fileName = "TerrainViewConfig", menuName = "FantasyMayor/Terrain/TerrainViewConfig")]
    public class TerrainViewConfig : ScriptableObject
    {
        [SerializeField]
        private int _subdivisions;
        [SerializeField]
        private float _cellSize;
        [SerializeField]
        private int _windErosionPass;
        [SerializeField]
        private float _heightScale = 0.25f;

        public float CellSize => _cellSize;
        public int Subdivisions => _subdivisions;
        public int WindErosionPass => _windErosionPass;
        public float HeightScale => _heightScale;
    }
}
