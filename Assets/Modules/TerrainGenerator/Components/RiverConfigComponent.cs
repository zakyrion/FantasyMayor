namespace Modules.TerrainGenerator.Components
{
    /// <summary>Stores river generation parameters extracted from <see cref="Configs.RiverConfig"/>.</summary>
    internal struct RiverConfigComponent
    {
        /// <summary>How many edge tiles to skip from each corner when selecting river endpoints.</summary>
        public int CornerOffsetTiles;
    }
}
