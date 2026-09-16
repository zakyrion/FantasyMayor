using EcsExtensions;
using Domains.Actors.City.Systems;
using Domains.Actors.Mayor.Systems;
using Modules.Boot.Core;
using VContainer;
using VContainer.Unity;
using Domains.Actors.City.Configs;
using Domains.Actors.Mayor.Configs;

namespace Domains.Actors.Installer
{
    public sealed class ActorsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterAppStateSystem<ConfigLoaderSystem<CityConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.CITY_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<MayorConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.MAYOR_CONFIG);

            builder.RegisterAppStateSystem<CitySpawnSystem>(Lifetime.Singleton, AppState.MapCreation);

            builder.RegisterAppStateSystem<MayorSpawnSystem>(Lifetime.Singleton, AppState.MapCreation);
        }
    }
}
