namespace Modules.TerrainGenerator.Components
{
    /// <summary>Stores lake generation parameters extracted from <see cref="Configs.LakeConfig"/>.</summary>
    internal struct LakeConfigComponent
    {
        /// <summary>Minimum distance in tiles from the map edge within which the lake centre cannot spawn.</summary>
        public int EdgeMarginTiles;

        /// <summary>Lake area as a fraction of the total tile count (0–1).</summary>
        public float SizeFraction;
    }
}
