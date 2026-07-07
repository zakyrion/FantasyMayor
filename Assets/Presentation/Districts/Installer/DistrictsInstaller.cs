using DefaultECSExtensions;
using Modules.Boot.Core;
using Presentation.Districts.Systems;
using VContainer;
using VContainer.Unity;

namespace Presentation.Districts.Installer
{
    // Presentation-side installer for the district-view vertical: the view-catalogue config loader (collected
    // into the ConfigLoadStep bootstrap) and the reactive DistrictViewSpawnSystem (registered concrete; Boot
    // wires it into the Gameplay state by hand, like the Forest reactive systems).
    public sealed class DistrictsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<DistrictViewsConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictViewsConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            // Reactive runtime spawner is event-driven (idle until a pulse). Concrete registration:
            // Boot wires it into the Gameplay state by hand.
            builder.Register<DistrictViewSpawnSystem>(Lifetime.Singleton)
                .As<DistrictViewSpawnSystem>();
        }
    }
}
