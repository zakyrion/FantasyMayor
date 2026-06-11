using DefaultECSExtensions;
using Modules.Boot.Core;
using Modules.HexesUI.Systems;
using VContainer;
using VContainer.Unity;

namespace Modules.HexesUI.Installer
{
    /// <summary>
    ///     Registers the HexesUI systems: the generator UI (MainMenu state), the info-panel config loader and
    ///     spawn step (collected by interface), and the info-panel per-frame systems (wired into Gameplay by Boot).
    /// </summary>
    public sealed class UIInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ShowHexesUISystem>(Lifetime.Scoped)
                .As<ShowHexesUISystem>();

            // Info panel — config loader (ConfigLoadStep) + view spawn (generation pipeline), collected by interface.
            builder.Register<HexTerrainIconConfigLoaderSystem>(Lifetime.Singleton)
                .As<HexTerrainIconConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();
            builder.Register<HexInfoPanelSpawnSystem>(Lifetime.Singleton)
                .As<HexInfoPanelSpawnSystem, IPrioritizedUniTaskSystem<TerrainGenerationStep>>();

            // Info panel — per-frame systems, wired into GameplayState by Boot (concrete singletons).
            builder.Register<HexInfoPanelSystem>(Lifetime.Singleton)
                .As<HexInfoPanelSystem>();
            builder.Register<HexInfoPanelHeaderSystem>(Lifetime.Singleton)
                .As<HexInfoPanelHeaderSystem>();
            builder.Register<HexInfoPanelResourcesSystem>(Lifetime.Singleton)
                .As<HexInfoPanelResourcesSystem>();
            builder.Register<HexInfoPanelDistrictPlaceholderSystem>(Lifetime.Singleton)
                .As<HexInfoPanelDistrictPlaceholderSystem>();
        }
    }
}
