using DefaultECSExtensions;
using Modules.Boot.Core;
using Presentation.UI.MainHud.ContextTabs.Systems;
using Presentation.UI.DistrictBuild.Systems;
using Presentation.UI.MainHud.TurnPanel.Systems;
using Presentation.UI.GeneratorMenu.Systems;
using Presentation.UI.MainHud.HexInfoPanel.Systems;
using Presentation.UI.MainHud.ResourceBar.Systems;
using Presentation.UI.MainHud.Systems;
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

            builder.Register<DistrictIconConfigLoaderSystem>(Lifetime.Singleton)
                .As<DistrictIconConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            builder.Register<InventoryResourceIconConfigLoaderSystem>(Lifetime.Singleton)
                .As<InventoryResourceIconConfigLoaderSystem, IUniTaskSystem<ConfigLoadStep>>();

            // Main UI spawn — orchestrator (generation pipeline, collected by interface) instantiates the
            // Main UI root and runs the window spawn subsystems (collected as MainHudSpawnSubSystem).
            builder.Register<MainHudSpawnSystem>(Lifetime.Singleton)
                .As<MainHudSpawnSystem, IPrioritizedUniTaskSystem<MapGenerationStep>>();
            builder.Register<HexInfoPanelSpawnSubSystem>(Lifetime.Singleton)
                .As<HexInfoPanelSpawnSubSystem, MainHudSpawnSubSystem>();
            builder.Register<TurnPanelSpawnSubSystem>(Lifetime.Singleton)
                .As<TurnPanelSpawnSubSystem, MainHudSpawnSubSystem>();
            builder.Register<ContextTabsSpawnSubSystem>(Lifetime.Singleton)
                .As<ContextTabsSpawnSubSystem, MainHudSpawnSubSystem>();
            builder.Register<ResourceBarSpawnSubSystem>(Lifetime.Singleton)
                .As<ResourceBarSpawnSubSystem, MainHudSpawnSubSystem>();

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
            // Section populators collected by DistrictBuildUISystem (IReadOnlyList<DistrictBuildUISubSystem>).
            builder.Register<DistrictBuildListUISubSystem>(Lifetime.Singleton)
                .As<DistrictBuildListUISubSystem, DistrictBuildUISubSystem>();
            builder.Register<DistrictBuildHexResourcesUISubSystem>(Lifetime.Singleton)
                .As<DistrictBuildHexResourcesUISubSystem, DistrictBuildUISubSystem>();
            builder.Register<DistrictBuildPriceUISubSystem>(Lifetime.Singleton)
                .As<DistrictBuildPriceUISubSystem, DistrictBuildUISubSystem>();
            builder.Register<DistrictBuildActionsUISubSystem>(Lifetime.Singleton)
                .As<DistrictBuildActionsUISubSystem, DistrictBuildUISubSystem>();
            builder.Register<TurnPanelViewSystem>(Lifetime.Singleton)
                .As<TurnPanelViewSystem>();
            builder.Register<ContextTabSelectionSystem>(Lifetime.Singleton)
                .As<ContextTabSelectionSystem>();
            builder.Register<ContextTabsAvailabilitySystem>(Lifetime.Singleton)
                .As<ContextTabsAvailabilitySystem>();
        }
    }
}
