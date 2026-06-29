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
            builder.Register<MapGenerationSystem>(Lifetime.Singleton)
                .As<MapGenerationSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<MountainGenerationSubSystem>(Lifetime.Singleton)
                .As<MountainGenerationSubSystem, MapGenerationSubSystem>();
            builder.Register<RiverGenerationSubSystem>(Lifetime.Singleton)
                .As<RiverGenerationSubSystem, MapGenerationSubSystem>();
            builder.Register<LakeGenerationSubSystem>(Lifetime.Singleton)
                .As<LakeGenerationSubSystem, MapGenerationSubSystem>();
            builder.Register<SeaGenerationSubSystem>(Lifetime.Singleton)
                .As<SeaGenerationSubSystem, MapGenerationSubSystem>();
        }
    }
}
