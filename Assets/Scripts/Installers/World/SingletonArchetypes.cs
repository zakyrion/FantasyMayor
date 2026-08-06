using Domains.Actions.Components;
using Domains.Actors.City.Components;
using Domains.Actors.Mayor.Components;
using Domains.Economy.District.Components;
using Domains.Economy.DistrictBuild.Components;
using Domains.Economy.DistrictBuildCost.Components;
using Domains.Economy.DistrictBuildOutcome.Components;
using Domains.Economy.DistrictOpenCondition.Components;
using Domains.Map.Generation.Components;
using Domains.Map.HexResources.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using Modules.Cameras.Components;
using Modules.Turn.Components;
using Modules.UserInput.Components;
using Presentation.Districts.Components;
using Presentation.HexIcons.Components;
using Presentation.HexResources.Components;
using Presentation.Terrain.Components;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.MainHud.Components;
using Presentation.UI.MainHud.ContextTabs.Components;
using Presentation.UI.MainHud.HexInfoPanel.Components;
using Presentation.UI.MainHud.ResourceBar.Components;

namespace Installers.World
{
    /// <summary>Declares the complete composition of the hidden singleton-components row.</summary>
    public static class SingletonArchetypes
    {
        public static SingletonArchetypeDefinition Singleton()
        {
            var componentTypes = default(ComponentTypes);

            componentTypes.Add<ActionIdAllocatorComponent>();

            componentTypes.Add<CityConfigComponent>();
            componentTypes.Add<CityIdAllocatorComponent>();
            componentTypes.Add<MayorConfigComponent>();
            componentTypes.Add<MayorIdAllocatorComponent>();

            componentTypes.Add<DistrictBuildsConfigComponent>();
            componentTypes.Add<DistrictBuildCostsConfigComponent>();
            componentTypes.Add<DistrictBuildOutcomesConfigComponent>();
            componentTypes.Add<DistrictIdAllocatorComponent>();
            componentTypes.Add<DistrictOpenConditionsConfigComponent>();

            componentTypes.Add<HexResourcesConfigComponent>();
            componentTypes.Add<LakeConfigComponent>();
            componentTypes.Add<MountainConfigComponent>();
            componentTypes.Add<RiverConfigComponent>();
            componentTypes.Add<SeaConfigComponent>();
            componentTypes.Add<TerrainGenerationConfigComponent>();

            componentTypes.Add<CameraComponent>();

            componentTypes.Add<TurnCountComponent>();
            componentTypes.Add<TurnProcessorComponent>();

            componentTypes.Add<CameraMovementConfigComponent>();

            componentTypes.Add<ClayViewConfigComponent>();
            componentTypes.Add<DistrictBuildProgressViewsConfigComponent>();
            componentTypes.Add<DistrictViewsConfigComponent>();
            componentTypes.Add<HeightSmoothingConfigComponent>();
            componentTypes.Add<HexIconsConfigComponent>();
            componentTypes.Add<HexIconsViewComponent>();
            componentTypes.Add<HexIconsVisibilityComponent>();
            componentTypes.Add<HexResourceIconConfigComponent>();
            componentTypes.Add<HexResourcesViewConfigComponent>();
            componentTypes.Add<HydraulicErosionConfigComponent>();
            componentTypes.Add<InnerIsolineConfigComponent>();
            componentTypes.Add<OuterIsolineConfigComponent>();
            componentTypes.Add<TerrainTextureComponent>();
            componentTypes.Add<TerrainTextureConfigComponent>();
            componentTypes.Add<TerrainViewConfigComponent>();
            componentTypes.Add<VertexGridComponent>();
            componentTypes.Add<WaterViewConfigComponent>();
            componentTypes.Add<WindErosionConfigComponent>();

            componentTypes.Add<ActiveContextTabComponent>();
            componentTypes.Add<ContextTabsViewComponent>();
            componentTypes.Add<DistrictBuildActionsUIViewComponent>();
            componentTypes.Add<DistrictBuildHexResourcesUIViewComponent>();
            componentTypes.Add<DistrictBuildListUIViewComponent>();
            componentTypes.Add<DistrictBuildPriceUIViewComponent>();
            componentTypes.Add<DistrictBuildUIRootComponent>();
            componentTypes.Add<DistrictIconConfigComponent>();
            componentTypes.Add<HexTerrainIconConfigComponent>();
            componentTypes.Add<InventoryResourceIconConfigComponent>();
            componentTypes.Add<MainHudComponent>();

            return new SingletonArchetypeDefinition(componentTypes, Tags.Get<SingletonTag>());
        }
    }
}
