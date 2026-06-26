using Presentation.HexResources.Configs;
using Unity.Mathematics;
using UnityEngine;

namespace Presentation.HexResources.Components
{
    /// <summary>
    ///     Flattened runtime view of <see cref="ClayViewConfig" />, read by
    ///     <see cref="Presentation.HexResources.Systems.ClayHexResourceViewSubSystem" />. Lives on its own
    ///     config singleton entity.
    /// </summary>
    internal struct ClayViewConfigComponent
    {
        public float DepressionRadius;
        public float DepressionDepth;
        public float2 FootprintAspect;
        public float PearFactor;
        public float NoiseAmplitude;
        public float NoiseFrequency;
        public Color ClayCenterColor;
        public Color ClayRimColor;

        public static ClayViewConfigComponent FromConfig(ClayViewConfig config)
        {
            return new ClayViewConfigComponent
            {
                DepressionRadius = config.DepressionRadius,
                DepressionDepth = config.DepressionDepth,
                FootprintAspect = new float2(config.FootprintAspect.x, config.FootprintAspect.y),
                PearFactor = config.PearFactor,
                NoiseAmplitude = config.NoiseAmplitude,
                NoiseFrequency = config.NoiseFrequency,
                ClayCenterColor = config.ClayCenterColor,
                ClayRimColor = config.ClayRimColor,
            };
        }
    }
}
