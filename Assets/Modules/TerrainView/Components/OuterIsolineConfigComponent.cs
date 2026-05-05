using Modules.TerrainView.Data;

namespace Modules.TerrainView.Components
{
    public struct OuterIsolineConfigComponent
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
        public float PreviewLengthMultiplier;
        public int PreviewSamplesPerUnit;

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
                PreviewLengthMultiplier = config.previewLengthMultiplier,
                PreviewSamplesPerUnit = config.previewSamplesPerUnit
            };
        }
    }
}
