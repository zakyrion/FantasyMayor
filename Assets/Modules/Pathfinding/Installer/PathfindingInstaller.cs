using VContainer;
using VContainer.Unity;

namespace Modules.Pathfinding.Installer
{
    public class PathfindingInstaller : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<HexPathfindingUtility>(Lifetime.Singleton)
                .As<HexPathfindingUtility, IHexPathfindingUtility>();
        }
    }
}
