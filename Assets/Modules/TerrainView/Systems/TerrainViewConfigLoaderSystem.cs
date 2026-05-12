using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.HexesCore.Utils;
using Modules.TerrainView.Components;
using Modules.TerrainView.Configs;
using Modules.TerrainView.Data;

namespace Modules.TerrainView.Systems
{
    [UsedImplicitly]
    public class TerrainViewConfigLoaderSystem : ConfigLoaderSystem
    {
        private const string HEIGHT_SMOOTH_CONFIG = "HeightSmoothingConfig";
        private const string HYDRAULIC_EROSION_CONFIG = "HydraulicErosionConfig";
        private const string INNER_ISOLINE_CONFIG = "InnerIsolineConfig";
        private const string OUTER_ISOLINE_CONFIG = "OuterIsolineConfig";
        private const string TERRAIN_TEXTURE_CONFIG = "TerrainTextureConfig";
        private const string TERRAIN_VIEW_CONFIG = "TerrainViewConfig";
        private const string WIND_EROSION_CONFIG = "WindErosionConfig";

        public TerrainViewConfigLoaderSystem(IAddressable addressable, World world)
            : base(addressable, world)
        {
        }

        protected override async UniTask LoadConfigsAsync(CancellationToken cancellationToken)
        {
            var innerIsolineConfig = Box<IsolineConfig>.Empty();
            var outerIsolineConfig = Box<IsolineConfig>.Empty();
            var heightSmoothingConfig = Box<HeightSmoothingConfig>.Empty();
            var windErosionConfig = Box<WindErosionConfig>.Empty();
            var hydraulicErosionConfig = Box<HydraulicErosionConfig>.Empty();
            var terrainViewConfig = Box<TerrainViewConfig>.Empty();
            var terrainTextureConfig = Box<TerrainTextureConfig>.Empty();

            try
            {
                (innerIsolineConfig, outerIsolineConfig, heightSmoothingConfig, windErosionConfig, hydraulicErosionConfig, terrainViewConfig, terrainTextureConfig) =
                    await UniTask.WhenAll(
                        LoadConfigAsync<IsolineConfig>(INNER_ISOLINE_CONFIG, cancellationToken),
                        LoadConfigAsync<IsolineConfig>(OUTER_ISOLINE_CONFIG, cancellationToken),
                        LoadConfigAsync<HeightSmoothingConfig>(HEIGHT_SMOOTH_CONFIG, cancellationToken),
                        LoadConfigAsync<WindErosionConfig>(WIND_EROSION_CONFIG, cancellationToken),
                        LoadConfigAsync<HydraulicErosionConfig>(HYDRAULIC_EROSION_CONFIG, cancellationToken),
                        LoadConfigAsync<TerrainViewConfig>(TERRAIN_VIEW_CONFIG, cancellationToken),
                        LoadConfigAsync<TerrainTextureConfig>(TERRAIN_TEXTURE_CONFIG, cancellationToken));

                if (cancellationToken.IsCancellationRequested)
                    return;

                World.CreateEntity().Set(InnerIsolineConfigComponent.FromConfig(innerIsolineConfig.Value));
                World.CreateEntity().Set(OuterIsolineConfigComponent.FromConfig(outerIsolineConfig.Value));
                World.CreateEntity().Set(HeightSmoothingConfigComponent.FromConfig(heightSmoothingConfig.Value));
                World.CreateEntity().Set(WindErosionConfigComponent.FromConfig(windErosionConfig.Value));
                World.CreateEntity().Set(HydraulicErosionConfigComponent.FromConfig(hydraulicErosionConfig.Value));
                World.CreateEntity().Set(TerrainViewConfigComponent.FromConfig(terrainViewConfig.Value));
                World.CreateEntity().Set(TerrainTextureConfigComponent.FromConfig(terrainTextureConfig.Value));

                var vertexGrid = new VertexGrid(
                    terrainViewConfig.Value.CellSize,
                    terrainViewConfig.Value.Subdivisions);
                World.CreateEntity().Set(new VertexGridComponent { Grid = vertexGrid });

                MarkAsLoaded();
            }
            finally
            {
                DisposeBox(ref innerIsolineConfig);
                DisposeBox(ref outerIsolineConfig);
                DisposeBox(ref heightSmoothingConfig);
                DisposeBox(ref windErosionConfig);
                DisposeBox(ref hydraulicErosionConfig);
                DisposeBox(ref terrainViewConfig);
                DisposeBox(ref terrainTextureConfig);
            }
        }
    }
}
