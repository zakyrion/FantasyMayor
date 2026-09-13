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
            builder.Register<ConfigLoaderSystem<CityConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.CITY_CONFIG);

            builder.Register<ConfigLoaderSystem<MayorConfig>>(Lifetime.Singleton)
                .As<IUniTaskSystem>()
                .WithParameter(AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.MAYOR_CONFIG);

            builder.Register<CitySpawnSystem>(Lifetime.Singleton)
                .As<CitySpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<MayorSpawnSystem>(Lifetime.Singleton)
                .As<MayorSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();
        }
    }
}
