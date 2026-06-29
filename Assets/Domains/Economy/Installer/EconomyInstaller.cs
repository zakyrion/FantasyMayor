using DefaultECSExtensions;
using Domains.Economy.District.Systems;
using Modules.Boot.Core;
using VContainer;
using VContainer.Unity;

namespace Domains.Economy.Installer
{
    // Economy ships the District config loader (catalogue of buildable districts). Resource is data types + the
    // generic ResourceLoadoutSpawner mechanism. District build/operate systems will register here later.
    public sealed class EconomyInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<DistrictsBuildConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictsBuildConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();
        }
    }
}
