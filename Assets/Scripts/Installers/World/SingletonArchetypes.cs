using Domains.Actions.Components;
using Domains.Actors.City.Components;
using Domains.Actors.Mayor.Components;
using Domains.Economy.District.Components;
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

            componentTypes.Add<CityIdAllocatorComponent>();
            componentTypes.Add<MayorIdAllocatorComponent>();

            componentTypes.Add<DistrictIdAllocatorComponent>();

            componentTypes.Add<CameraComponent>();

            componentTypes.Add<TurnCountComponent>();
            componentTypes.Add<TurnProcessorComponent>();

            componentTypes.Add<HexIconsViewComponent>();
            componentTypes.Add<HexIconsVisibilityComponent>();
            componentTypes.Add<TerrainTextureComponent>();
            componentTypes.Add<VertexGridComponent>();

            componentTypes.Add<ActiveContextTabComponent>();
            componentTypes.Add<ContextTabsViewComponent>();
            componentTypes.Add<DistrictBuildActionsUIViewComponent>();
            componentTypes.Add<DistrictBuildHexResourcesUIViewComponent>();
            componentTypes.Add<DistrictBuildListUIViewComponent>();
            componentTypes.Add<DistrictBuildPriceUIViewComponent>();
            componentTypes.Add<DistrictBuildUIRootComponent>();
            componentTypes.Add<MainHudComponent>();

            return new SingletonArchetypeDefinition(componentTypes, Tags.Get<SingletonTag>());
        }
    }
}
