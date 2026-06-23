using DefaultECSExtensions;
using Domains.Actors.Systems;
using Modules.Boot.Core;
using VContainer;
using VContainer.Unity;

namespace Domains.Actors.Installer
{
    public sealed class ActorsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ActorsSpawnSystem>(Lifetime.Singleton)
                .As<ActorsSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();
        }
    }
}
