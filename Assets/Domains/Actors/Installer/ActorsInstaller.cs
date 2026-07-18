using EcsExtensions;
using Domains.Actors.City.Systems;
using Domains.Actors.Mayor.Systems;
using Modules.Boot.Core;
using VContainer;
using VContainer.Unity;

namespace Domains.Actors.Installer
{
    public sealed class ActorsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<CityConfigLoaderSystem>(Lifetime.Singleton)
                .As<CityConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<MayorConfigLoaderSystem>(Lifetime.Singleton)
                .As<MayorConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<CitySpawnSystem>(Lifetime.Singleton)
                .As<CitySpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            builder.Register<MayorSpawnSystem>(Lifetime.Singleton)
                .As<MayorSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();
        }
    }
}
