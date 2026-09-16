using EcsExtensions;
using Modules.Boot.Core;
using Presentation.HexResources.Systems;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Presentation.HexResources.Configs;

namespace Presentation.HexResources.Installer
{
    public sealed class HexResourcesViewInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterAppStateSystem<ConfigLoaderSystem<HexResourcesViewConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEX_RESOURCES_VIEW_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<ClayViewConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.CLAY_VIEW_CONFIG);

            builder.RegisterAppStateSystem<HexResourcesViewSystem>(Lifetime.Singleton, AppState.MapCreation);

            // Reactive runtime forest systems are event-driven (idle until a pulse). A first-order per-frame
            // system: its state membership is its AppState flags, resolved through IAppStateSystem.
            builder.RegisterAppStateSystem<ForestSpawnSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<ForestDespawnSystem>(Lifetime.Singleton, AppState.Gameplay);

            builder.Register<ForestHexResourceViewSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<ClayHexResourceViewSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<FishHexResourceViewSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
        }
    }
}
