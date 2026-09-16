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
            builder.RegisterAppStateSystem<ConfigLoaderSystem<InnerIsolineConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.INNER_ISOLINE_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<OuterIsolineConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.OUTER_ISOLINE_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<HeightSmoothingConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEIGHT_SMOOTHING_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<WindErosionConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.WIND_EROSION_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<HydraulicErosionConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HYDRAULIC_EROSION_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<TerrainViewConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.TERRAIN_VIEW_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<TerrainTextureConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.TERRAIN_TEXTURE_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<WaterViewConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.WATER_VIEW_CONFIG);

            builder.RegisterAppStateSystem<VertexGridSpawnSystem>(Lifetime.Singleton, AppState.InstanceObjects);

            builder.RegisterAppStateSystem<TerrainViewSystem>(Lifetime.Singleton, AppState.MapCreation);
            builder.RegisterAppStateSystem<HexSelectionViewLoadingSystem>(Lifetime.Singleton, AppState.MapCreation);
            // A first-order per-frame system: its state membership is its AppState flags, resolved through
            // IAppStateSystem — no state names it by hand.
            builder.RegisterAppStateSystem<HexSelectionViewSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<TerrainViewDebugSystem>(Lifetime.Singleton, AppState.MapCreation);

            builder.Register<TerrainViewGenerationSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<TerrainViewTextureSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<WaterViewSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
        }
    }
}
