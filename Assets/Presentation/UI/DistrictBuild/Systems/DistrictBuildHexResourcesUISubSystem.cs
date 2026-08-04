using System;
using Friflo.Engine.ECS;
using Domains.Economy.District.Data;
using Domains.Economy.District.Helpers;
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
using Presentation.UI.DistrictBuild.Tags;
using Presentation.UI.DistrictBuild.Views;
using UnityEngine;
using EcsExtensions;
using Presentation.Terrain.Tags;
using Domains.Map.HexResources.Tags;

namespace Presentation.UI.DistrictBuild.Systems
{
    // ВИМОГИ populator: reconciles the requirements section to the current DistrictBuildSelectionComponent — the
    // selected district's name header + one ✓/✕ line per active gate dimension (terrain / empty-hex / required
    // resource), tested against the selected hex. Reads ECS directly (no shared read-model). The per-line marks
    // mirror DistrictBuildingConfig.CanBuildOn so they can never disagree with the gate.
    [UsedImplicitly]
    public sealed class DistrictBuildHexResourcesUISubSystem : DistrictBuildUISubSystem
    {
        private readonly ArchetypeQuery _selectionSet;
        private readonly ArchetypeQuery _selectedHexSet;
        private readonly ArchetypeQuery _hexSet;
        // HexIdFKComponent is shared by every hex-anchored entity kind (views, containers, districts) — a bare
        // ComponentIndex over it is ambiguous across kinds; scope the query to the resource archetype itself.
        private readonly ArchetypeQuery _hexResources;

        public override int Priority => SystemPriorities.SubSystems.DistrictBuildUi.HexResources;

        public DistrictBuildHexResourcesUISubSystem(EntityStore world) : base(world)
        {
            _selectionSet = world.Query().AllTags(Friflo.Engine.ECS.Tags.Get<DistrictBuildSelectionTag>());
            _selectedHexSet = world.Query<HexSelectedComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexSelectionTag>());
            _hexSet = world.Query<HexIdComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexTag>());
            _hexResources = world.Query<HexIdFKComponent, HexResourceComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexResourceTag>());
        }

        public override void Populate(GameObject root)
        {
            var view = World.GetWorldComponent<DistrictBuildHexResourcesUIViewComponent>().View;

            if (!_selectionSet.TryGetFirst(out var selectionEntity))
                throw new InvalidOperationException(
                    $"DistrictBuildHexResourcesUISubSystem: Populate called with no active {nameof(DistrictBuildSelectionTag)} entity.");

            var selected = selectionEntity.GetComponent<DistrictBuildSelectionComponent>().Selected;
            if (!TryGetDistrict(selected, out var district))
            {
                view.SetDistrictName(string.Empty);
                view.ClearRequirements();
                return;
            }

            view.SetDistrictName(DistrictBuildLabels.DistrictName(district.DistrictType));
            view.ClearRequirements();

            if (!_selectedHexSet.TryGetFirst(out var selectedHexEntity))
                return;

            var coords = selectedHexEntity.GetComponent<HexSelectedComponent>().Coords;
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

            if (type == DistrictType.None || !World.HasWorldComponent<DistrictBuildsConfigComponent>())
                return false;

            return DistrictConfigLookup.TryFind(
                World.GetWorldComponent<DistrictBuildsConfigComponent>().Value?.Districts, type, d => d.DistrictType, out district);
        }

        private bool TryGetHexType(HexCoord coords, out HexType type)
        {
            type = default;
            foreach (var hexEntity in _hexSet.Entities)
            {
                if (hexEntity.GetComponent<HexIdComponent>().Coords != coords)
                    continue;

                type = hexEntity.GetComponent<HexTypeComponent>().Type;
                return true;
            }

            return false;
        }

        private int HexResourceCount(HexCoord coords)
        {
            var count = 0;
            foreach (var resource in _hexResources.Entities)
                if (resource.GetComponent<HexIdFKComponent>().Coords.Equals(coords))
                    count++;

            return count;
        }

        private bool HexHasResource(HexCoord coords, HexResourceType type)
        {
            foreach (var resource in _hexResources.Entities)
            {
                if (!resource.GetComponent<HexIdFKComponent>().Coords.Equals(coords))
                    continue;

                if (resource.GetComponent<HexResourceComponent>().Type == type)
                    return true;
            }

            return false;
        }
    }
}
