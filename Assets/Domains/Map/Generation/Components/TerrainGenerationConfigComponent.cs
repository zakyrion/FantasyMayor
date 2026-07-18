using Domains.Map.Generation.Configs;
using Domains.Map.Generation.Data;
using Friflo.Engine.ECS;

namespace Domains.Map.Generation.Components
{
    public struct TerrainGenerationConfigComponent : IComponent
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
