using Friflo.Engine.ECS;
using Presentation.Terrain.Data;

namespace Presentation.Terrain.Components
{
    public struct OuterIsolineConfigComponent : IComponent
    {
        public TraversalSideData Side;
        public int DepthCenter;
        public int DepthDeviation;
        public int CurveSeed;
        public float BaseFrequency;
        public int Octaves;
        public float Persistence;
        public float Lacunarity;
        public int SmoothingPasses;

        public static OuterIsolineConfigComponent FromConfig(IsolineConfig config)
        {
            return new OuterIsolineConfigComponent
            {
                Side = config.side,
                DepthCenter = config.depthCenter,
                DepthDeviation = config.depthDeviation,
                CurveSeed = config.curveSeed,
                BaseFrequency = config.baseFrequency,
                Octaves = config.octaves,
                Persistence = config.persistence,
                Lacunarity = config.lacunarity,
                SmoothingPasses = config.smoothingPasses,
            };
        }
    }
}
