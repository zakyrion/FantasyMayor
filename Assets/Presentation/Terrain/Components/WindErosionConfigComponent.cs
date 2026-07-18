using Friflo.Engine.ECS;
using Presentation.Terrain.Data;

namespace Presentation.Terrain.Components
{
    public struct WindErosionConfigComponent : IComponent
    {
        public bool EnableWindErosion;
        public float WindStrength;
        public float WindTransportRate;
        public int WindErosionIterations;
        public float WindMaxTransportPerIteration;

        public static WindErosionConfigComponent FromConfig(WindErosionConfig config)
        {
            return new WindErosionConfigComponent
            {
                EnableWindErosion = config.EnableWindErosion,
                WindStrength = config.WindStrength,
                WindTransportRate = config.WindTransportRate,
                WindErosionIterations = config.WindErosionIterations,
                WindMaxTransportPerIteration = config.WindMaxTransportPerIteration
            };
        }
    }
}
