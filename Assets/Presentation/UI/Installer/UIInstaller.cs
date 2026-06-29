using DefaultECSExtensions;
using Modules.Boot.Core;
using Presentation.UI.ContextTabs.Systems;
using Presentation.UI.DistrictBuild.Systems;
using Presentation.UI.EndTurn.Systems;
using Presentation.UI.GeneratorMenu.Systems;
using Presentation.UI.HexInfoPanel.Systems;
using Presentation.UI.ResourceBar.Systems;
using Presentation.UI.Systems;
using VContainer;
using VContainer.Unity;

namespace Presentation.UI.Installer
{
    /// <summary>
    ///     Registers the MainUI systems: the generator UI (MainMenu state), the info-panel config loader, the
    ///     Main UI spawn orchestrator + its window spawn subsystems (generation pipeline), and the per-frame
    ///     view systems (info panel + end-turn state) — the per-frame systems are wired into Gameplay by Boot.
    /// </summary>
    public sealed class UIInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ShowHexesUISystem>(Lifetime.Scoped)
                .As<ShowHexesUISystem>();

            builder.Register<HexTerrainIconConfigLoaderSystem>(Lifetime.Singleton)
                .As<HexTerrainIconConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<InventoryResourceIconConfigLoaderSystem>(Lifetime.Singleton)
                .As<InventoryResourceIconConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            // Main UI spawn — orchestrator (generation pipeline, collected by interface) instantiates the
            // Main UI root and runs the window spawn subsystems (collected as MainUISpawnSubSystem).
            builder.Register<MainUISpawnSystem>(Lifetime.Singleton)
                .As<MainUISpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();
            builder.Register<HexInfoPanelSpawnSubSystem>(Lifetime.Singleton)
                .As<HexInfoPanelSpawnSubSystem, MainUISpawnSubSystem>();
            builder.Register<EndTurnSpawnSubSystem>(Lifetime.Singleton)
                .As<EndTurnSpawnSubSystem, MainUISpawnSubSystem>();
            builder.Register<ContextTabsSpawnSubSystem>(Lifetime.Singleton)
                .As<ContextTabsSpawnSubSystem, MainUISpawnSubSystem>();
            builder.Register<ResourceBarSpawnSubSystem>(Lifetime.Singleton)
                .As<ResourceBarSpawnSubSystem, MainUISpawnSubSystem>();

            // District-build overlay — its OWN UIDocument (separate from the shared Main UI), so it has its own
            // spawn orchestrator in the generation pipeline rather than a Main UI spawn subsystem.
            builder.Register<DistrictBuildUISpawnSystem>(Lifetime.Singleton)
                .As<DistrictBuildUISpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();

            // Per-frame view systems, wired into GameplayState by Boot (concrete singletons).
            builder.Register<HexInfoPanelSystem>(Lifetime.Singleton)
                .As<HexInfoPanelSystem>();
            builder.Register<HexInfoPanelHeaderSystem>(Lifetime.Singleton)
                .As<HexInfoPanelHeaderSystem>();
            builder.Register<HexInfoPanelResourcesSystem>(Lifetime.Singleton)
                .As<HexInfoPanelResourcesSystem>();
            builder.Register<ResourceBarSystem>(Lifetime.Singleton)
                .As<ResourceBarSystem>();
            builder.Register<HexInfoPanelDistrictSystem>(Lifetime.Singleton)
                .As<HexInfoPanelDistrictSystem>();
            builder.Register<DistrictBuildUISystem>(Lifetime.Singleton)
                .As<DistrictBuildUISystem>();
            builder.Register<EndTurnViewSystem>(Lifetime.Singleton)
                .As<EndTurnViewSystem>();
            builder.Register<ContextTabSelectionSystem>(Lifetime.Singleton)
                .As<ContextTabSelectionSystem>();
            builder.Register<ContextTabsAvailabilitySystem>(Lifetime.Singleton)
                .As<ContextTabsAvailabilitySystem>();
        }
    }
}
