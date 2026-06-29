using DefaultECSExtensions;
using Modules.Boot.Core;
using Presentation.HexIcons.Systems;
using VContainer;
using VContainer.Unity;

namespace Presentation.HexIcons.Installer
{
    public sealed class HexIconsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<HexIconsConfigLoaderSystem>(Lifetime.Singleton)
                .As<HexIconsConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<HexIconsSpawnSystem>(Lifetime.Singleton)
                .As<HexIconsSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            // Per-frame positioner — a concrete singleton wired into GameplayState by Boot (like ForestSpawnSystem).
            builder.Register<HexIconsContainerPositionSystem>(Lifetime.Singleton)
                .As<HexIconsContainerPositionSystem>();

            // Event-driven icon renderer — a concrete singleton wired into GameplayState by Boot.
            builder.Register<HexIconsVisibilitySystem>(Lifetime.Singleton)
                .As<HexIconsVisibilitySystem>();
        }
    }
}
