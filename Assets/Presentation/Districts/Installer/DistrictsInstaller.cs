using EcsExtensions;
using Modules.Boot.Core;
using Presentation.Districts.Systems;
using VContainer;
using VContainer.Unity;
using Presentation.Districts.Configs;

namespace Presentation.Districts.Installer
{
    // Presentation-side installer for the district-view vertical: the view-catalogue configs (loaded at
    // AppState.ConfigLoading) and the reactive spawner/despawner systems (registered concrete; Boot
    // wires them into the Gameplay state by hand, like the Forest reactive systems).
    public sealed class DistrictsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ConfigLoaderSystem<DistrictViewsConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_VIEWS_CONFIG);

            builder.Register<ConfigLoaderSystem<DistrictBuildProgressViewsConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_BUILD_PROGRESS_VIEWS_CONFIG);

            // Reactive runtime spawner/despawner are event-driven (idle until a pulse). Concrete registration:
            // Boot wires them into the Gameplay state by hand.
            builder.Register<DistrictViewSpawnSystem>(Lifetime.Singleton)
                .As<DistrictViewSpawnSystem>();

            builder.Register<DistrictBuildProgressViewSpawnSystem>(Lifetime.Singleton)
                .As<DistrictBuildProgressViewSpawnSystem>();

            builder.Register<DistrictBuildProgressViewDespawnSystem>(Lifetime.Singleton)
                .As<DistrictBuildProgressViewDespawnSystem>();
        }
    }
}
