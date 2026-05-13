using Modules.TerrainView.Configs;
using UnityEngine;

namespace Modules.TerrainView.Components
{
    /// <summary>
    ///     Flattened ECS snapshot of <see cref="WaterViewConfig" />.
    ///     Created once by the config loader; consumed by <see cref="Systems.WaterViewSubSystem" />.
    /// </summary>
    public struct WaterViewConfigComponent
    {
        public float WaterYOffset;
        public int Subdivisions;

        public Color ShallowColor;
        public Color DeepColor;

        /// <summary>
        ///     Reference to the wave normal map texture. Kept alive by the managed reference
        ///     even after the Addressable config box is disposed.
        /// </summary>
        public Texture2D WaveNormalMap;

        public float WaveAmplitude;
        public float WaveFrequency;
        public float WaveSpeed;
        public Vector2 WaveScale;
        public Vector2 WaveScale2;

        public Vector2 FlowDirection;
        public float FlowSpeed;

        public float FoamStrength;
        public float FoamWidth;

        public float FresnelPower;
        public float Transparency;

        /// <summary>
        ///     Creates a flattened component from the addressable-loaded ScriptableObject.
        /// </summary>
        /// <param name="config">Source config asset.</param>
        /// <returns>Value-type snapshot safe for ECS storage.</returns>
        public static WaterViewConfigComponent FromConfig(WaterViewConfig config)
        {
            return new WaterViewConfigComponent
            {
                WaterYOffset = config.waterYOffset,
                Subdivisions = config.subdivisions,
                ShallowColor = config.shallowColor,
                DeepColor = config.deepColor,
                WaveNormalMap = config.waveNormalMap,
                WaveAmplitude = config.waveAmplitude,
                WaveFrequency = config.waveFrequency,
                WaveSpeed = config.waveSpeed,
                WaveScale = config.waveScale,
                WaveScale2 = config.waveScale2,
                FlowDirection = config.flowDirection,
                FlowSpeed = config.flowSpeed,
                FoamStrength = config.foamStrength,
                FoamWidth = config.foamWidth,
                FresnelPower = config.fresnelPower,
                Transparency = config.transparency
            };
        }
    }
}
