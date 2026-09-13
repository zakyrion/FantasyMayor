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
            builder.Register<ConfigLoaderSystem<HexIconsConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEX_ICONS_CONFIG);

            builder.Register<ConfigLoaderSystem<HexResourceIconConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEX_RESOURCE_ICON_CONFIG);

            builder.Register<HexIconsSpawnSystem>(Lifetime.Singleton)
                .As<HexIconsSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            // Per-frame positioner — a concrete singleton wired into GameplayState by Boot (like ForestSpawnSystem).
            builder.Register<HexIconsContainerPositionSystem>(Lifetime.Singleton)
                .As<HexIconsContainerPositionSystem>();

            // Event-driven icon renderer — a concrete singleton wired into GameplayState by Boot.
            builder.Register<HexIconsVisibilitySystem>(Lifetime.Singleton)
                .As<HexIconsVisibilitySystem>();
        }
    }
}
