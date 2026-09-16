using EcsExtensions;
using Modules.Boot.Core;
using Presentation.Districts.Systems;
using VContainer;
using VContainer.Unity;
using Presentation.Districts.Configs;

namespace Presentation.Districts.Installer
{
    // Presentation-side installer for the district-view vertical: the view-catalogue configs (loaded at
    // AppState.ConfigLoading) and the reactive spawner/despawner systems (first-order Gameplay systems,
    // exposed as IAppStateSystem — GameplayState keeps them by its AppState flags, no hand wiring).
    public sealed class DistrictsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterAppStateSystem<ConfigLoaderSystem<DistrictViewsConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_VIEWS_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<DistrictBuildProgressViewsConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_BUILD_PROGRESS_VIEWS_CONFIG);

            // Reactive runtime spawner/despawner are event-driven (idle until a pulse). First-order Gameplay
            // registration: their state membership is their AppState flags, resolved through IAppStateSystem.
            builder.RegisterAppStateSystem<DistrictViewSpawnSystem>(Lifetime.Singleton, AppState.Gameplay);

            builder.RegisterAppStateSystem<DistrictBuildProgressViewSpawnSystem>(Lifetime.Singleton, AppState.Gameplay);

            builder.RegisterAppStateSystem<DistrictBuildProgressViewDespawnSystem>(Lifetime.Singleton, AppState.Gameplay);
        }
    }
}
