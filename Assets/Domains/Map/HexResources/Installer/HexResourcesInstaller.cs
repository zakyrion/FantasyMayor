using EcsExtensions;
using Modules.Boot.Core;
using Domains.Map.HexResources.Systems;
using VContainer;
using VContainer.Unity;
using Domains.Map.HexResources.Configs;

namespace Domains.Map.HexResources.Installer
{
    public sealed class HexResourcesInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ConfigLoaderSystem<HexResourcesConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEX_RESOURCES_CONFIG);

            builder.Register<HexResourcesSystem>(Lifetime.Singleton)
                .As<HexResourcesSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<ForestResourceGenerationSubSystem>(Lifetime.Singleton)
                .As<ForestResourceGenerationSubSystem, HexResourcesSubSystem>();
            builder.Register<ClayResourceGenerationSubSystem>(Lifetime.Singleton)
                .As<ClayResourceGenerationSubSystem, HexResourcesSubSystem>();
            builder.Register<FishResourceGenerationSubSystem>(Lifetime.Singleton)
                .As<FishResourceGenerationSubSystem, HexResourcesSubSystem>();
        }
    }
}
