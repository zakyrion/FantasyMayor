namespace Modules.TerrainGenerator.Components
{
    /// <summary>Stores sea generation parameters extracted from <see cref="Configs.SeaConfig"/>.</summary>
    internal struct SeaConfigComponent
    {
        /// <summary>Sea area as a fraction of the total tile count (0–1).</summary>
        public float SizeFraction;
    }
}
