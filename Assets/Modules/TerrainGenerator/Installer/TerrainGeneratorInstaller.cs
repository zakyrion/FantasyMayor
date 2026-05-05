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

            builder.Register<MountainGenerationSystem>(Lifetime.Singleton)
                .As<MountainGenerationSystem, GenerationSystem>();
            builder.Register<RiverGenerationSystem>(Lifetime.Singleton)
                .As<RiverGenerationSystem, GenerationSystem>();
            builder.Register<LakeGenerationSystem>(Lifetime.Singleton)
                .As<LakeGenerationSystem, GenerationSystem>();
            builder.Register<SeaGenerationSystem>(Lifetime.Singleton)
                .As<SeaGenerationSystem, GenerationSystem>();
        }
    }
}
