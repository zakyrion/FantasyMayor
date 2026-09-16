using EcsExtensions;
using Modules.Boot.Core;
using Presentation.HexIcons.Systems;
using VContainer;
using VContainer.Unity;
using Presentation.HexIcons.Configs;

namespace Presentation.HexIcons.Installer
{
    public sealed class HexIconsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterAppStateSystem<ConfigLoaderSystem<HexIconsConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEX_ICONS_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<HexResourceIconConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEX_RESOURCE_ICON_CONFIG);

            builder.RegisterAppStateSystem<HexIconsSpawnSystem>(Lifetime.Singleton, AppState.MapCreation);

            // Per-frame positioner — a first-order system, its state membership is its AppState flags.
            builder.RegisterAppStateSystem<HexIconsContainerPositionSystem>(Lifetime.Singleton, AppState.Gameplay);

            // Event-driven icon renderer — a first-order system, its state membership is its AppState flags.
            builder.RegisterAppStateSystem<HexIconsVisibilitySystem>(Lifetime.Singleton, AppState.Gameplay);
        }
    }
}
