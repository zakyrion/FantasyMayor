using Domains.Map.Generation.Configs;
using Domains.Map.Generation.Data;

namespace Domains.Map.Generation.Components
{
    public struct TerrainGenerationConfigComponent
    {
        public int WaveCount;
        public WaterType WaterType;

        public static TerrainGenerationConfigComponent FromConfig(TerrainGenerationConfig config)
        {
            return new TerrainGenerationConfigComponent
            {
                WaveCount = config.WaveCount,
                WaterType = config.WaterType
            };
        }
    }
}
