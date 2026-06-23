using DefaultECSExtensions;
using Modules.Boot.Core;
using Presentation.Terrain.Systems;
using VContainer;
using VContainer.Unity;

namespace Presentation.Terrain.Installer
{
    /// <summary>
    ///     Configures the hex-related services, config-load systems, and per-frame view systems
    ///     that belong to the TerrainView module.
    /// </summary>
    public sealed class TerrainViewInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<TerrainViewConfigLoaderSystem>(Lifetime.Singleton)
                .As<TerrainViewConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();
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
