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
            builder.Register<ConfigLoaderSystem<HexResourcesViewConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEX_RESOURCES_VIEW_CONFIG);

            builder.Register<ConfigLoaderSystem<ClayViewConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.CLAY_VIEW_CONFIG);

            builder.Register<HexResourcesViewSystem>(Lifetime.Singleton)
                .As<HexResourcesViewSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            // Reactive runtime forest systems are event-driven (idle until a pulse). Concrete registration:
            // Boot wires them into the Gameplay state by hand.
            builder.Register<ForestSpawnSystem>(Lifetime.Singleton)
                .As<ForestSpawnSystem>();
            builder.Register<ForestDespawnSystem>(Lifetime.Singleton)
                .As<ForestDespawnSystem>();

            builder.Register<ForestHexResourceViewSubSystem>(Lifetime.Singleton)
                .As<ForestHexResourceViewSubSystem, HexResourcesViewSubSystem>();
            builder.Register<ClayHexResourceViewSubSystem>(Lifetime.Singleton)
                .As<ClayHexResourceViewSubSystem, HexResourcesViewSubSystem>();
            builder.Register<FishHexResourceViewSubSystem>(Lifetime.Singleton)
                .As<FishHexResourceViewSubSystem, HexResourcesViewSubSystem>();
        }
    }
}
