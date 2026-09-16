using EcsExtensions;
using Modules.Boot.Core;
using Domains.Map.Generation.Systems;
using VContainer;
using VContainer.Unity;
using Domains.Map.Generation.Configs;

namespace Domains.Map.Generation.Installer
{
    public sealed class TerrainGeneratorInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterAppStateSystem<ConfigLoaderSystem<TerrainGenerationConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.TERRAIN_GENERATION_CONFIG);
            builder.RegisterAppStateSystem<GenerationSystem>(Lifetime.Singleton, AppState.MapCreation);

            builder.Register<MountainGenerationSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<RiverGenerationSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<LakeGenerationSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<SeaGenerationSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
        }
    }
}
