using EcsExtensions;
using Modules.Boot.Core;
using Presentation.Districts.Systems;
using VContainer;
using VContainer.Unity;

namespace Presentation.Districts.Installer
{
    // Presentation-side installer for the district-view vertical: the view-catalogue config loaders (collected
    // into the ConfigLoadStep bootstrap) and the reactive spawner/despawner systems (registered concrete; Boot
    // wires them into the Gameplay state by hand, like the Forest reactive systems).
    public sealed class DistrictsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<DistrictViewsConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictViewsConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<DistrictBuildProgressViewsConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictBuildProgressViewsConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

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
