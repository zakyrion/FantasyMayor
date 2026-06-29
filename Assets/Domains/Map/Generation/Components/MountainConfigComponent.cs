namespace Domains.Map.Generation.Components
{
    /// <summary>Stores mountain generation parameters extracted from <see cref="Configs.TerrainGenerationConfig"/>.</summary>
    internal struct MountainConfigComponent
    {
        /// <summary>Mountain coverage as a fraction of the total tile count (0–1).</summary>
        public float SizeFraction;

        /// <summary>Number of independent growth seeds that merge into the final mountain body.</summary>
        public int SeedCount;

        /// <summary>Maximum axial distance allowed between any seed and its nearest neighbour seed.</summary>
        public int MaxSeedDistance;

        /// <summary>Minimum number of Level 2 neighbours required for a Level 0 hex to become a foothill.</summary>
        public int MinFoothillNeighbors;

        /// <summary>Minimum number of foothills that must be placed around the mountain.</summary>
        public int MinFoothillCount;

        /// <summary>Minimum contour distance between any two mandatory foothills.</summary>
        public int MinFoothillDistance;
    }
}
