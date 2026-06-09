using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.HexIcons.Systems;
using VContainer;
using VContainer.Unity;

namespace Modules.HexIcons.Installer
{
    public sealed class HexIconsInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<HexIconsConfigLoaderSystem>(Lifetime.Singleton)
                .As<HexIconsConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<HexIconsSpawnSystem>(Lifetime.Singleton)
                .As<HexIconsSpawnSystem, IPrioritizedUniTaskSystem<TerrainGenerationStep>>();

            // Per-frame positioner — a concrete singleton wired into GameplayState by Boot (like ForestViewSyncSystem).
            builder.Register<HexIconsContainerPositionSystem>(Lifetime.Singleton)
                .As<HexIconsContainerPositionSystem>();

            // Event-driven icon renderer — a concrete singleton wired into GameplayState by Boot.
            builder.Register<HexIconsVisibilitySystem>(Lifetime.Singleton)
                .As<HexIconsVisibilitySystem>();
        }
    }
}
