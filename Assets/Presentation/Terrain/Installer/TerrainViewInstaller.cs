using EcsExtensions;
using Modules.Boot.Core;
using Presentation.Terrain.Systems;
using VContainer;
using VContainer.Unity;
using Presentation.Terrain.Configs;
using Presentation.Terrain.Data;

namespace Presentation.Terrain.Installer
{
    /// <summary>
    ///     Configures the TerrainView configs, the vertex-grid instance step, and the generation and per-frame
    ///     view systems that belong to the TerrainView module.
    /// </summary>
    public sealed class TerrainViewInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ConfigLoaderSystem<InnerIsolineConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.INNER_ISOLINE_CONFIG);

            builder.Register<ConfigLoaderSystem<OuterIsolineConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.OUTER_ISOLINE_CONFIG);

            builder.Register<ConfigLoaderSystem<HeightSmoothingConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEIGHT_SMOOTHING_CONFIG);

            builder.Register<ConfigLoaderSystem<WindErosionConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.WIND_EROSION_CONFIG);

            builder.Register<ConfigLoaderSystem<HydraulicErosionConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HYDRAULIC_EROSION_CONFIG);

            builder.Register<ConfigLoaderSystem<TerrainViewConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.TERRAIN_VIEW_CONFIG);

            builder.Register<ConfigLoaderSystem<TerrainTextureConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.TERRAIN_TEXTURE_CONFIG);

            builder.Register<ConfigLoaderSystem<WaterViewConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.WATER_VIEW_CONFIG);

            builder.Register<VertexGridSpawnSystem>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.InstanceObjects);

            builder.Register<TerrainViewSystem>(Lifetime.Singleton)
                .As<TerrainViewSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();
            builder.Register<HexSelectionViewLoadingSystem>(Lifetime.Singleton)
                .As<HexSelectionViewLoadingSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();
            // Concrete registration: Boot wires this per-frame system into game states by hand.
            builder.Register<HexSelectionViewSystem>(Lifetime.Singleton)
                .As<HexSelectionViewSystem>();
            builder.Register<TerrainViewDebugSystem>(Lifetime.Singleton)
                .As<TerrainViewDebugSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<TerrainViewGenerationSubSystem>(Lifetime.Singleton)
                .As<TerrainViewGenerationSubSystem, ViewSubSystem>();
            builder.Register<TerrainViewTextureSubSystem>(Lifetime.Singleton)
                .As<TerrainViewTextureSubSystem, ViewSubSystem>();
            builder.Register<WaterViewSubSystem>(Lifetime.Singleton)
                .As<WaterViewSubSystem, ViewSubSystem>();
        }
    }
}
