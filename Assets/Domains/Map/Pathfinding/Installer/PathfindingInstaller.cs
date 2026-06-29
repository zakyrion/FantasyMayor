using VContainer;
using VContainer.Unity;

namespace Domains.Map.Pathfinding.Installer
{
    public sealed class PathfindingInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<HexPathfindingUtility>(Lifetime.Singleton)
                .As<HexPathfindingUtility, IHexPathfindingUtility>();
        }
    }
}
