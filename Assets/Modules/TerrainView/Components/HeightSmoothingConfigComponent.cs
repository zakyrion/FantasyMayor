using Modules.TerrainView.Data;

namespace Modules.TerrainView.Components
{
    public struct HeightSmoothingConfigComponent
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
