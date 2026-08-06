using System;
using Domains.Economy.Archetypes;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Economy.District.Helpers;
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
    ///     Reactive runtime construction-progress spawner. Anchored on the one-frame
    ///     <see cref="DistrictTableChangedEvent" /> pulse (FLOW_DISTRICT_BUILD unification, 2026-07-17 — was
    ///     <c>DistrictBuildConfirmedEvent</c>, a COMMAND, which coupled this system to
    ///     <c>BuildDistrictActionSystem</c>'s tick order): on its presence it reconciles state — every District
    ///     row staged <c>DistrictBuildState.Planned</c> whose hex has no progress view yet gets its prefab
    ///     instantiated on the hex centre. Works with current world state, not the pulse payload, so it is
    ///     idempotent regardless of which stage transition raised the pulse. Torn down by
    ///     <see cref="DistrictBuildProgressViewDespawnSystem" /> at build completion (R1) or cancel (R5).
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictBuildProgressViewSpawnSystem : UpdatedSystem
    {
        // District rows: one per hex, carrying the hex FK, its type, and its build stage.
        private readonly Archetype _districts;

        private readonly EntityStorages _storages;

        // Progress-view entities -> lets the reconcile skip hexes already viewed; also the birth archetype for
        // new views. HexIdFKComponent is shared by every hex-anchored entity kind (the District row itself
        // included), so a bare ComponentIndex over it would always see the District row and never spawn a view.
        private readonly Archetype _viewArchetype;

        private UnityEngine.Transform _root;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildProgressViewSpawn;

        public DistrictBuildProgressViewSpawnSystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<DistrictTableChangedEvent>(storages.World))
        {
            _storages = storages;

            _districts = EconomyArchetypes.District(storages.World);
            _viewArchetype = PresentationArchetypes.DistrictBuildProgressView(storages.World);
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            if (!_storages.Singletons.Has<DistrictBuildProgressViewsConfigComponent>())
                throw new InvalidOperationException(
                    "DistrictBuildProgressViewSpawnSystem: DistrictBuildProgressViewsConfigComponent singleton component is missing.");

            if (!_storages.Singletons.Has<VertexGridComponent>())
                throw new InvalidOperationException(
                    "DistrictBuildProgressViewSpawnSystem: VertexGridComponent singleton component is missing.");

            var viewsConfig = _storages.Singletons.Get<DistrictBuildProgressViewsConfigComponent>().Value;
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

                    if (district.GetComponent<DistrictBuildStateComponent>().Value != DistrictBuildState.Planned)
                        continue;

                    var hexId = district.GetComponent<HexIdFKComponent>();
                    if (HasView(hexId.Coords))
                        continue;

                    var districtType = district.GetComponent<DistrictTypeComponent>().Value;
                    var prefab = ResolvePrefab(viewsConfig, districtType);

                    var centerCoord = vertexGrid.GetCenterVertexCoord(hexId.Coords);
                    if (!vertexGrid.TryGet(centerCoord, out var centerVertex))
                        throw new InvalidOperationException(
                            $"DistrictBuildProgressViewSpawnSystem: hex {hexId.Coords} has no centre vertex on the grid.");

                    if (_root == null)
                        _root = new GameObject("DistrictBuildProgressViewRoot").transform;

                    Vector3 worldPos = centerVertex.Position;
                    var instance = Object.Instantiate(prefab, worldPos, Quaternion.identity, _root);
                    var view = instance.GetComponent<DistrictBuildProgressView>();

                    var viewEntity = _viewArchetype.CreateEntity();
                    viewEntity.AddComponent(new HexIdFKComponent { Coords = hexId.Coords });
                    viewEntity.AddComponent(
                        new DistrictBuildProgressViewComponent { Type = districtType, View = view });
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

        // Fail loud: an in-progress build with no configured progress prefab is an authoring gap, not a benign skip.
        private GameObject ResolvePrefab(DistrictBuildProgressViewsConfig viewsConfig, DistrictType districtType)
        {
            if (!DistrictConfigLookup.TryFind(
                    viewsConfig.Views, districtType, v => v.DistrictType, v => v.Prefab != null, out var match))
                throw new InvalidOperationException(
                    $"DistrictBuildProgressViewSpawnSystem: no progress prefab configured for district type '{districtType}'.");

            return match.Prefab;
        }
    }
}
