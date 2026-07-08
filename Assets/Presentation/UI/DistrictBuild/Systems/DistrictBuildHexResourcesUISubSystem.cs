using System;
using DefaultEcs;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.DistrictBuild.Components;
using Domains.Economy.DistrictBuild.Configs;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Tags;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Terrain.Components;
using Presentation.UI.DistrictBuild.Components;
using Presentation.UI.DistrictBuild.Views;
using UnityEngine;
using DefaultECSExtensions;

namespace Presentation.UI.DistrictBuild.Systems
{
    // ВИМОГИ populator: reconciles the requirements section to the current DistrictBuildSelectionComponent — the
    // selected district's name header + one ✓/✕ line per active gate dimension (terrain / empty-hex / required
    // resource), tested against the selected hex. Reads ECS directly (no shared read-model). The per-line marks
    // mirror DistrictBuildingConfig.CanBuildOn so they can never disagree with the gate.
    [UsedImplicitly]
    public sealed class DistrictBuildHexResourcesUISubSystem : DistrictBuildUISubSystem
    {
        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _hexSet;
        private readonly EntityMultiMap<HexIdComponent> _hexResources;

        public override int Priority => SystemPriorities.SubSystems.DistrictBuildUi.HexResources;

        public DistrictBuildHexResourcesUISubSystem(World world) : base(world)
        {
            _selectedHexSet = world.GetEntities().With<HexSelectedComponent>().AsSet();
            _hexSet = world.GetEntities().With<HexTag>().With<HexIdComponent>().AsSet();
            _hexResources = world.GetEntities()
                .With<HexResourceComponent>().With<HexIdComponent>().AsMultiMap<HexIdComponent>();
        }

        public override void Populate(GameObject root)
        {
            var view = World.Get<DistrictBuildHexResourcesUIViewComponent>().View;

            var selected = World.Get<DistrictBuildSelectionComponent>().Selected;
            if (!TryGetDistrict(selected, out var district))
            {
                view.SetDistrictName(string.Empty);
                view.ClearRequirements();
                return;
            }

            view.SetDistrictName(DistrictBuildLabels.DistrictName(district.DistrictType));
            view.ClearRequirements();

            if (_selectedHexSet.Count == 0)
                return;

            var coords = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>().Coords;
            if (!TryGetHexType(coords, out var hexType))
                return;

            if (district.ImpossibleToBuildTypes.Count > 0)
                view.AddRequirement(
                    $"Заборонений терен: {DistrictBuildLabels.HexTypeLabels(district.ImpossibleToBuildTypes)}",
                    !district.ImpossibleToBuildTypes.Contains(hexType));

            if (district.NeedEmptyHexResourcesToBuild)
                view.AddRequirement("Гекс без ресурсів", HexResourceCount(coords) == 0);
            else if (district.RequiredHexResourceType != HexResourceType.Unknown)
                view.AddRequirement(
                    $"Потрібен ресурс: {DistrictBuildLabels.HexResourceLabel(district.RequiredHexResourceType)}",
                    HexHasResource(coords, district.RequiredHexResourceType));
        }

        private bool TryGetDistrict(DistrictType type, out DistrictBuildConfig district)
        {
            district = null;
            if (type == DistrictType.Unknown)
                throw new InvalidOperationException(
                    $"{nameof(DistrictBuildSelectionComponent)}.{nameof(DistrictBuildSelectionComponent.Selected)} " +
                    $"is {DistrictType.Unknown} — the selection must be a real district or {nameof(DistrictType.None)}, " +
                    "never the error marker.");

            if (type == DistrictType.None || !World.Has<DistrictBuildsConfigComponent>())
                return false;

            var districts = World.Get<DistrictBuildsConfigComponent>().Value?.Districts;
            if (districts == null)
                return false;

            for (var i = 0; i < districts.Length; i++)
                if (districts[i] != null && districts[i].DistrictType == type)
                {
                    district = districts[i];
                    return true;
                }

            return false;
        }

        private bool TryGetHexType(HexCoord coords, out HexType type)
        {
            type = default;
            foreach (var hexEntity in _hexSet.GetEntities())
            {
                if (hexEntity.Get<HexIdComponent>().Coords != coords)
                    continue;

                type = hexEntity.Get<HexTypeComponent>().Type;
                return true;
            }

            return false;
        }

        private int HexResourceCount(HexCoord coords) =>
            _hexResources.TryGetEntities(new HexIdComponent { Coords = coords }, out var resources)
                ? resources.Length
                : 0;

        private bool HexHasResource(HexCoord coords, HexResourceType type)
        {
            if (!_hexResources.TryGetEntities(new HexIdComponent { Coords = coords }, out var resources))
                return false;

            foreach (var resource in resources)
                if (resource.Get<HexResourceComponent>().Type == type)
                    return true;

            return false;
        }

        public override void Dispose()
        {
            _selectedHexSet.Dispose();
            _hexSet.Dispose();
            _hexResources.Dispose();
            base.Dispose();
        }
    }
}
