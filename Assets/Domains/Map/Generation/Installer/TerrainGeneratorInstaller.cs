using DefaultECSExtensions;
using Modules.Boot.Core;
using Domains.Map.Generation.Systems;
using VContainer;
using VContainer.Unity;

namespace Domains.Map.Generation.Installer
{
    public sealed class TerrainGeneratorInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<TerrainGenerationConfigLoaderSystem>(Lifetime.Singleton)
                .As<TerrainGenerationConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();
            builder.Register<GenerationSystem>(Lifetime.Singleton)
                .As<GenerationSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<MountainGenerationSubSystem>(Lifetime.Singleton)
                .As<MountainGenerationSubSystem, GenerationSubSystem>();
            builder.Register<RiverGenerationSubSystem>(Lifetime.Singleton)
                .As<RiverGenerationSubSystem, GenerationSubSystem>();
            builder.Register<LakeGenerationSubSystem>(Lifetime.Singleton)
                .As<LakeGenerationSubSystem, GenerationSubSystem>();
            builder.Register<SeaGenerationSubSystem>(Lifetime.Singleton)
                .As<SeaGenerationSubSystem, GenerationSubSystem>();
        }
    }
}
