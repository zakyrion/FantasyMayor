using Friflo.Engine.ECS;
using Presentation.Terrain.Data;

namespace Presentation.Terrain.Components
{
    public struct HeightSmoothingConfigComponent : IComponent
    {
        public bool EnableHeightBlur;
        public int HeightBlurRadius;
        public int HeightBlurIterations;

        public static HeightSmoothingConfigComponent FromConfig(HeightSmoothingConfig config)
        {
            return new HeightSmoothingConfigComponent
            {
                EnableHeightBlur = config.EnableHeightBlur,
                HeightBlurRadius = config.HeightBlurRadius,
                HeightBlurIterations = config.HeightBlurIterations
            };
        }
    }
}
