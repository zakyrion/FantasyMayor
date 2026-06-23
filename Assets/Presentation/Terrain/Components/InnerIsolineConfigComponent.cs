using Presentation.Terrain.Data;

namespace Presentation.Terrain.Components
{
    public struct InnerIsolineConfigComponent
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

        public static InnerIsolineConfigComponent FromConfig(IsolineConfig config)
        {
            return new InnerIsolineConfigComponent
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
