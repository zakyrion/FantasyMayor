using DefaultECSExtensions;
using VContainer;
using VContainer.Unity;

namespace Installers.World
{
    public class WorldInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(new DefaultEcs.World());
            builder.Register<EventCleanupSystem>(Lifetime.Singleton).As<UpdatedSystem, IUpdatedSystem>();
        }
    }
}
