using EcsExtensions;
using Modules.Boot.Core;
using Presentation.UI.MainHud.ContextTabs.Systems;
using Presentation.UI.DistrictBuild.Systems;
using Presentation.UI.MainHud.TurnPanel.Systems;
using Presentation.UI.GeneratorMenu.Systems;
using Presentation.UI.MainHud.HexInfoPanel.Systems;
using Presentation.UI.MainHud.ResourceBar.Systems;
using Presentation.UI.MainHud.Systems;
using Presentation.UI.MainHud.Components;
using Presentation.UI.DistrictBuild.Components;
using VContainer;
using VContainer.Unity;
using Presentation.UI.MainHud.HexInfoPanel.Configs;
using Presentation.UI.MainHud.ResourceBar.Configs;

namespace Presentation.UI.Installer
{
    /// <summary>
    ///     Registers the MainUI systems: the generator UI (AppState.MainMenu), the icon configs
    ///     (AppState.ConfigLoading), the Main UI and district-build overlay spawn stages (AppState.MapCreation)
    ///     plus their sub-system families on the sub-system contract, and the per-frame/reactive view systems
    ///     (AppState.Gameplay) — each registration carries its own AppState flags, so no state is wired by hand.
    /// </summary>
    public sealed class UIInstaller : IInstaller
    {
        public void Install(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                var storages = container.Resolve<EntityStorages>();
                storages.Singletons.Set(new MainHudComponent());
                storages.Singletons.Set(new DistrictBuildUIRootComponent());
            });

            builder.RegisterAppStateSystem<ShowHexesUISystem>(Lifetime.Scoped, AppState.MainMenu);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<HexTerrainIconConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.HEX_TERRAIN_ICON_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<DistrictIconConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.DISTRICT_ICON_CONFIG);

            builder.RegisterAppStateSystem<ConfigLoaderSystem<InventoryResourceIconConfig>>(Lifetime.Singleton, AppState.ConfigLoading)
                .WithParameter("address", ConfigAddresses.INVENTORY_RESOURCE_ICON_CONFIG);

            // Main UI spawn — a map-creation pipeline stage (AppState.MapCreation) that instantiates the Main UI
            // root and runs its kept sub-systems (MainHudSpawnSubSystem, collected on the sub-system contract).
            builder.RegisterAppStateSystem<MainHudSpawnSystem>(Lifetime.Singleton, AppState.MapCreation);
            builder.Register<HexInfoPanelSpawnSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<TurnPanelSpawnSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<ContextTabsSpawnSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<ResourceBarSpawnSubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();

            // District-build overlay — its OWN UIDocument (separate from the shared Main UI), so it has its own
            // spawn stage in the map-creation pipeline rather than a Main UI spawn subsystem.
            builder.RegisterAppStateSystem<DistrictBuildUISpawnSystem>(Lifetime.Singleton, AppState.MapCreation);

            // Per-frame and reactive view systems — AppState.Gameplay flags their own registration; no state
            // filters or wires them by hand.
            builder.RegisterAppStateSystem<HexInfoPanelSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<HexInfoPanelHeaderSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<HexInfoPanelResourcesSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<ResourceBarSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<HexInfoPanelDistrictSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<DistrictBuildUISystem>(Lifetime.Singleton, AppState.Gameplay);
            // Section populators, kept by DistrictBuildUISystem through the sub-system contract (OrchestratorType
            // == typeof(DistrictBuildUISystem)).
            builder.Register<DistrictBuildListUISubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<DistrictBuildHexResourcesUISubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<DistrictBuildPriceUISubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.Register<DistrictBuildActionsUISubSystem>(Lifetime.Singleton)
                .As<IPrioritizedUniTaskSystem>();
            builder.RegisterAppStateSystem<TurnPanelViewSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<ContextTabSelectionSystem>(Lifetime.Singleton, AppState.Gameplay);
            builder.RegisterAppStateSystem<ContextTabsAvailabilitySystem>(Lifetime.Singleton, AppState.Gameplay);
        }
    }
}
