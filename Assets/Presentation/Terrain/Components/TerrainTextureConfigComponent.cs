using Presentation.Terrain.Configs;
using UnityEngine;

namespace Presentation.Terrain.Components
{
    /// <summary>
    ///     Flattened ECS snapshot of <see cref="TerrainTextureConfig" />.
    ///     Created once by the config loader; consumed by <see cref="Systems.TerrainViewTextureSubSystem" />.
    /// </summary>
    public struct TerrainTextureConfigComponent
    {
        public int TextureResolution;
        public int BrushRadius;
        public int TextureBlurRadius;
        public float CenterWeight;
        public float EdgeWeight;
        public float SlopeThreshold;
        public float HueJitterStrength;

        public Color PlainColor;
        public Color MountainFlatColor;
        public Color MountainSteepColor;
        public Color BedhillColor;
        public Color CoastlineColor;
        public Color WaterColor;
        public Color FallbackColor;

        /// <summary>
        ///     Creates a flattened component from the addressable-loaded ScriptableObject.
        /// </summary>
        /// <param name="config">Source config asset.</param>
        /// <returns>Value-type snapshot safe for ECS storage.</returns>
        public static TerrainTextureConfigComponent FromConfig(TerrainTextureConfig config)
        {
            return new TerrainTextureConfigComponent
            {
                TextureResolution = config.textureResolution,
                BrushRadius = config.brushRadius,
                TextureBlurRadius = config.textureBlurRadius,
                CenterWeight = config.centerWeight,
                EdgeWeight = config.edgeWeight,
                SlopeThreshold = config.slopeThreshold,
                HueJitterStrength = config.hueJitterStrength,
                PlainColor = config.plainColor,
                MountainFlatColor = config.mountainFlatColor,
                MountainSteepColor = config.mountainSteepColor,
                BedhillColor = config.bedhillColor,
                CoastlineColor = config.coastlineColor,
                WaterColor = config.waterColor,
                FallbackColor = config.fallbackColor
            };
        }
    }
}
