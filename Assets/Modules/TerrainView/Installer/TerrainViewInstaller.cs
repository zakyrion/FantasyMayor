using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.TerrainView.Systems;
using VContainer;
using VContainer.Unity;

namespace Installers.TerrainView
{
    /// <summary>
    ///     Configures the hex-related services, config-load systems, and per-frame view systems
    ///     that belong to the TerrainView module.
    /// </summary>
    public class TerrainViewInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);
            builder.Register<TerrainViewConfigLoaderSystem>(Lifetime.Singleton)
                .As<TerrainViewConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();
            builder.Register<TerrainViewSystem>(Lifetime.Singleton)
                .As<TerrainViewSystem, IUpdatedSystem>();
            builder.Register<TerrainViewDebugSystem>(Lifetime.Singleton)
                .As<TerrainViewDebugSystem, IUpdatedSystem>();

            builder.Register<TerrainViewGenerationSubSystem>(Lifetime.Singleton)
                .As<TerrainViewGenerationSubSystem, ViewSubSystem>();
            builder.Register<TerrainViewTextureSubSystem>(Lifetime.Singleton)
                .As<TerrainViewTextureSubSystem, ViewSubSystem>();
            builder.Register<WaterViewSubSystem>(Lifetime.Singleton)
                .As<WaterViewSubSystem, ViewSubSystem>();
        }
    }
}
