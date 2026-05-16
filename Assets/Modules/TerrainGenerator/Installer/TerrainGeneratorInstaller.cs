using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.TerrainGenerator.Systems;
using VContainer;
using VContainer.Unity;

namespace Modules.TerrainGenerator.Installer
{
    public class TerrainGeneratorInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.Register<TerrainGenerationConfigLoaderSystem>(Lifetime.Singleton)
                .As<TerrainGenerationConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();
            builder.Register<TerrainGenerationSystem>(Lifetime.Singleton)
                .As<TerrainGenerationSystem, IUpdatedSystem>();

            builder.Register<MountainGenerationSubSystem>(Lifetime.Singleton)
                .As<MountainGenerationSubSystem, TerrainGenerationSubSystem>();
            builder.Register<RiverGenerationSubSystem>(Lifetime.Singleton)
                .As<RiverGenerationSubSystem, TerrainGenerationSubSystem>();
            builder.Register<LakeGenerationSubSystem>(Lifetime.Singleton)
                .As<LakeGenerationSubSystem, TerrainGenerationSubSystem>();
            builder.Register<SeaGenerationSubSystem>(Lifetime.Singleton)
                .As<SeaGenerationSubSystem, TerrainGenerationSubSystem>();
        }
    }
}
