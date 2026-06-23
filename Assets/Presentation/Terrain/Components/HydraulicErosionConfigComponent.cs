using Presentation.Terrain.Data;

namespace Presentation.Terrain.Components
{
    public struct HydraulicErosionConfigComponent
    {
        public bool EnableHydraulicErosion;
        public int HydraulicIterations;
        public float HydraulicRainAmount;
        public float HydraulicFlowRate;
        public float HydraulicEvaporation;
        public float HydraulicSedimentCapacity;
        public float HydraulicErosionRate;
        public float HydraulicDepositionRate;

        public static HydraulicErosionConfigComponent FromConfig(HydraulicErosionConfig config)
        {
            return new HydraulicErosionConfigComponent
            {
                EnableHydraulicErosion = config.EnableHydraulicErosion,
                HydraulicIterations = config.HydraulicIterations,
                HydraulicRainAmount = config.HydraulicRainAmount,
                HydraulicFlowRate = config.HydraulicFlowRate,
                HydraulicEvaporation = config.HydraulicEvaporation,
                HydraulicSedimentCapacity = config.HydraulicSedimentCapacity,
                HydraulicErosionRate = config.HydraulicErosionRate,
                HydraulicDepositionRate = config.HydraulicDepositionRate
            };
        }
    }
}
