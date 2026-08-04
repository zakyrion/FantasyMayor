using System;
using System.Collections.Generic;
using EcsExtensions;
using Friflo.Engine.ECS;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Economy.District.Helpers;
using Domains.Economy.District.Tags;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Districts.Components;
using Presentation.Districts.Configs;
using Presentation.Districts.Tags;
using Presentation.Districts.Views;
using Presentation.Terrain.Components;
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
        private readonly ArchetypeQuery _districts;

        // Progress-view entities -> lets the reconcile skip hexes already viewed. HexIdFKComponent is shared by
        // every hex-anchored entity kind (the District row itself included), so a bare ComponentIndex over it
        // would always see the District row and never spawn a view — scope the query to the view archetype.
        private readonly ArchetypeQuery _views;

        private readonly EntityStore _world;

        private UnityEngine.Transform _root;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildProgressViewSpawn;

        public DistrictBuildProgressViewSpawnSystem(EntityStore world)
            : base(world.Query<DistrictTableChangedEvent>())
        {
            _world = world;

            _districts = world.Query<HexIdFKComponent, DistrictTypeComponent, DistrictBuildStateComponent>()
                .AllTags(Friflo.Engine.ECS.Tags.Get<DistrictTag>());

            _views = world.Query<HexIdFKComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<DistrictBuildProgressViewTag>());
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            if (!_world.HasWorldComponent<DistrictBuildProgressViewsConfigComponent>())
                throw new InvalidOperationException(
                    "DistrictBuildProgressViewSpawnSystem: DistrictBuildProgressViewsConfigComponent world component is missing.");

            if (!_world.HasWorldComponent<VertexGridComponent>())
                throw new InvalidOperationException(
                    "DistrictBuildProgressViewSpawnSystem: VertexGridComponent world component is missing.");

            var viewsConfig = _world.GetWorldComponent<DistrictBuildProgressViewsConfigComponent>().Value;
            var vertexGrid = _world.GetWorldComponent<VertexGridComponent>().Grid;

            // AddComponent/AddTag on the freshly spawned view entity is a structural change while
            // _districts.Entities is enumerating (StructuralChangeException, store-wide) — collect spawn
            // data first, wire components after the query loop closes. Managed exception to the
            // "Unity.Collections in ECS systems" rule: View is a MonoBehaviour reference, which a
            // NativeContainer cannot hold. See ARCHITECTURE.md (collections rule).
            var pending = new List<(int Id, HexCoord Coords, DistrictType Type, DistrictBuildProgressView View)>();

            foreach (var district in _districts.Entities)
            {
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

                var viewEntity = _world.CreateEntity();
                pending.Add((viewEntity.Id, hexId.Coords, districtType, view));
            }

            foreach (var (id, coords, type, view) in pending)
            {
                _world.TryGetEntityById(id, out var viewEntity);
                viewEntity.AddComponent(new HexIdFKComponent { Coords = coords });
                viewEntity.AddComponent(new DistrictBuildProgressViewComponent { Type = type, View = view });
                viewEntity.AddTag<DistrictBuildProgressViewTag>();
            }
        }

        private bool HasView(HexCoord coords)
        {
            foreach (var view in _views.Entities)
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
