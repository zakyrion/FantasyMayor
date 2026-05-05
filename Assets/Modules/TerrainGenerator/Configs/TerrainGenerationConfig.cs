using Modules.TerrainGenerator.Data;
using UnityEngine;

namespace Modules.TerrainGenerator.Configs
{
    [CreateAssetMenu(fileName = "TerrainGenerationConfig", menuName = "FantasyMayor/Terrain/TerrainGenerationConfig")]
    internal class TerrainGenerationConfig : ScriptableObject
    {
        [Header("Spawn Settings")]
        [SerializeField] private int _waveCount = 5;

        [Header("Hill Settings")]
        [SerializeField] [Range(0f, 1f)] private float _hillSizeFraction = 0.05f;
        [SerializeField] private int _seedCount = 3;
        [SerializeField] private int _maxSeedDistance = 4;
        [SerializeField] [Range(1, 6)] private int _minFoothillNeighbors = 3;
        [SerializeField] private int _minFoothillCount = 3;
        [SerializeField] private int _minFoothillDistance = 4;

        [Header("Water Settings")]
        [SerializeField] private WaterType _waterType = WaterType.None;
        [SerializeField] private RiverConfig _riverConfig;
        [SerializeField] private LakeConfig _lakeConfig;
        [SerializeField] private SeaConfig _seaConfig;

        /// <summary>River config. Only relevant when <see cref="WaterType" /> is <see cref="Data.WaterType.River" />.</summary>
        public RiverConfig RiverConfig => _riverConfig;

        /// <summary>Lake config. Only relevant when <see cref="WaterType" /> is <see cref="Data.WaterType.Lake" />.</summary>
        public LakeConfig LakeConfig => _lakeConfig;

        /// <summary>Sea config. Only relevant when <see cref="WaterType" /> is <see cref="Data.WaterType.Sea" />.</summary>
        public SeaConfig SeaConfig => _seaConfig;
        public float HillSizeFraction => _hillSizeFraction;

        /// <summary>Number of independent growth seeds that merge into the final mountain body.</summary>
        public int SeedCount => _seedCount;

        /// <summary>Maximum axial distance allowed between any seed and its nearest neighbour seed.</summary>
        public int MaxSeedDistance => _maxSeedDistance;

        /// <summary>Minimum number of Level 2 neighbours required for a Level 0 hex to become a foothill.</summary>
        public int MinFoothillNeighbors => _minFoothillNeighbors;

        /// <summary>Minimum number of foothills that must be placed around the mountain.</summary>
        public int MinFoothillCount => _minFoothillCount;

        /// <summary>Minimum contour distance between any two mandatory foothills.</summary>
        public int MinFoothillDistance => _minFoothillDistance;

        public WaterType WaterType => _waterType;

        public int WaveCount => _waveCount;
    }
}
