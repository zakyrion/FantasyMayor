using DefaultECSExtensions;
using Domains.Economy.Resource.Systems;
using Modules.Boot.Core;
using VContainer;
using VContainer.Unity;

namespace Domains.Economy.Installer
{
    public sealed class EconomyInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<MayorConfigLoaderSystem>(Lifetime.Singleton)
                .As<MayorConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<ResourceInitSpawnSystem>(Lifetime.Singleton)
                .As<ResourceInitSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();
        }
    }
}
