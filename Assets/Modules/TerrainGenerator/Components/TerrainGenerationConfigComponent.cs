using Modules.TerrainGenerator.Configs;
using Modules.TerrainGenerator.Data;

namespace Modules.TerrainGenerator.Components
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
