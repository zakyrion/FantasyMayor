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
            builder.RegisterAppStateSystem<ConfigLoaderSystem<HexResourcesConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEX_RESOURCES_CONFIG);

            builder.RegisterAppStateSystem<HexResourcesSystem>(Lifetime.Singleton, AppState.MapCreation);

            builder.Register<ForestResourceGenerationSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<ClayResourceGenerationSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<FishResourceGenerationSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
        }
    }
}
