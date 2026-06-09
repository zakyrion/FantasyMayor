using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.HexResources.Systems;
using VContainer;
using VContainer.Unity;

namespace Modules.HexResources.Installer
{
    public sealed class HexResourcesInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<HexResourcesConfigLoaderSystem>(Lifetime.Singleton)
                .As<HexResourcesConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<HexResourcesSystem>(Lifetime.Singleton)
                .As<HexResourcesSystem, IPrioritizedUniTaskSystem<TerrainGenerationStep>>();

            builder.Register<ForestResourceGenerationSubSystem>(Lifetime.Singleton)
                .As<ForestResourceGenerationSubSystem, HexResourcesSubSystem>();
            builder.Register<ClayResourceGenerationSubSystem>(Lifetime.Singleton)
                .As<ClayResourceGenerationSubSystem, HexResourcesSubSystem>();
            builder.Register<FishResourceGenerationSubSystem>(Lifetime.Singleton)
                .As<FishResourceGenerationSubSystem, HexResourcesSubSystem>();
        }
    }
}
