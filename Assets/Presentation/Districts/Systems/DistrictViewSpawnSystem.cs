using System;
using Domains.Economy.Archetypes;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Map.Hex.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Archetypes;
using Presentation.Districts.Components;
using Presentation.Districts.Configs;
using Presentation.Districts.Views;
using Presentation.Terrain.Components;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Presentation.Districts.Systems
{
    /// <summary>
    ///     Reactive runtime district-view spawner. Anchored on the one-frame
    ///     <see cref="DistrictTableChangedEvent" /> pulse (folds the former <c>DistrictBuiltEvent</c>,
    ///     FLOW_DISTRICT_BUILD unification, 2026-07-17): on its presence it reconciles state — every District row
    ///     staged <c>DistrictBuildState.Built</c> that has no view yet gets its prefab instantiated on the hex
    ///     centre. The stage filter matters now the row exists from CONFIRM (<c>Planned</c>), not just at
    ///     completion — an unfiltered scan would spawn the finished-district prefab on rows still under
    ///     construction. Works with current world state, not the pulse payload, so it is idempotent: a second
    ///     pulse in the same frame finds nothing missing and no-ops.
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictViewSpawnSystem : UpdatedSystem
    {
        // District rows: one per hex, carrying the hex FK, its type, and its build stage.
        private readonly Archetype _districts;

        private readonly EntityStorages _storages;

        // District view entities -> lets the reconcile skip hexes already viewed; also the birth archetype for
        // new views. HexIdFKComponent is shared by every hex-anchored entity kind (the District row itself
        // included), so a bare ComponentIndex over it would always see the District row and never spawn a view.
        private readonly Archetype _viewArchetype;

        private UnityEngine.Transform _root;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictViewSpawn;

        public DistrictViewSpawnSystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<DistrictTableChangedEvent>(storages.World))
        {
            _storages = storages;

            _districts = EconomyArchetypes.District(storages.World);
            _viewArchetype = PresentationArchetypes.DistrictView(storages.World);
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            if (!_storages.Singletons.Has<DistrictViewsConfigComponent>())
                throw new InvalidOperationException(
                    "DistrictViewSpawnSystem: DistrictViewsConfigComponent singleton component is missing.");

            if (!_storages.Singletons.Has<VertexGridComponent>())
                throw new InvalidOperationException(
                    "DistrictViewSpawnSystem: VertexGridComponent singleton component is missing.");

            var viewsConfig = _storages.Singletons.Get<DistrictViewsConfigComponent>().Value;
            var vertexGrid = _storages.Singletons.Get<VertexGridComponent>().Grid;

            // Snapshot-before-iterate: birth via _viewArchetype.CreateEntity() is NOT a structural change,
            // but the AddComponent writes that follow it are, and they would throw while _districts.Entities
            // enumerates (ECS_CONVENTIONS → Structural changes during iteration). Snapshotting the District
            // ids closes that enumeration, so spawn and wiring both happen in one pass — no managed buffer.
            var districtIds = new NativeList<int>(Math.Max(1, _districts.Count), Allocator.Temp);
            try
            {
                foreach (var district in _districts.Entities)
                    districtIds.Add(district.Id);

                for (var i = 0; i < districtIds.Length; i++)
                {
                    if (!_storages.World.TryGetEntityById(districtIds[i], out var district))
                        continue;

                    if (district.GetComponent<DistrictBuildStateComponent>().Value != DistrictBuildState.Built)
                        continue;

                    var hexId = district.GetComponent<HexIdFKComponent>();
                    if (HasView(hexId.Coords))
                        continue;

                    var districtType = district.GetComponent<DistrictTypeComponent>().Value;
                    var prefab = ResolvePrefab(viewsConfig, districtType);

                    var centerCoord = vertexGrid.GetCenterVertexCoord(hexId.Coords);
                    if (!vertexGrid.TryGet(centerCoord, out var centerVertex))
                        throw new InvalidOperationException(
                            $"DistrictViewSpawnSystem: hex {hexId.Coords} has no centre vertex on the grid.");

                    if (_root == null)
                        _root = new GameObject("DistrictViewRoot").transform;

                    Vector3 worldPos = centerVertex.Position;
                    var instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, _root);
                    var view = instance.GetComponent<DistrictView>();

                    var viewEntity = _viewArchetype.CreateEntity();
                    viewEntity.AddComponent(new HexIdFKComponent { Coords = hexId.Coords });
                    viewEntity.AddComponent(new DistrictViewComponent { Type = districtType, View = view });
                }
            }
            finally
            {
                districtIds.Dispose();
            }
        }

        private bool HasView(HexCoord coords)
        {
            foreach (var view in _viewArchetype.Entities)
                if (view.GetComponent<HexIdFKComponent>().Coords.Equals(coords))
                    return true;

            return false;
        }

        // Fail loud: a built district with no configured view prefab is an authoring gap, not a benign skip.
        private GameObject ResolvePrefab(DistrictViewsConfig viewsConfig, DistrictType districtType)
        {
            var views = viewsConfig.Views;
            for (var i = 0; i < views.Length; i++)
                if (views[i].DistrictType == districtType && views[i].Prefab != null)
                    return views[i].Prefab;

            throw new InvalidOperationException(
                $"DistrictViewSpawnSystem: no view prefab configured for district type '{districtType}'.");
        }
    }
}
