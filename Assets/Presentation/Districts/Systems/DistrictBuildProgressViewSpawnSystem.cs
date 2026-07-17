using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Events;
using Domains.Economy.District.Helpers;
using Domains.Economy.District.Tags;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
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
        private readonly EntitySet _districts;

        // Progress-view entities indexed by the hex FK -> lets the reconcile skip hexes already viewed.
        private readonly EntityMultiMap<HexIdFKComponent> _viewsByHex;

        private readonly World _world;

        private Transform _root;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildProgressViewSpawn;

        public DistrictBuildProgressViewSpawnSystem(World world)
            : base(world.GetEntities()
                .With<DistrictTableChangedEvent>()
                .AsSet())
        {
            _world = world;

            _districts = world.GetEntities()
                .With<DistrictTag>()
                .With<HexIdFKComponent>()
                .With<DistrictTypeComponent>()
                .With<DistrictBuildStateComponent>()
                .AsSet();

            _viewsByHex = world.GetEntities()
                .With<HexIdFKComponent>()
                .With<DistrictBuildProgressViewComponent>().With<DistrictBuildProgressViewTag>()
                .AsMultiMap<HexIdFKComponent>();
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!_world.Has<DistrictBuildProgressViewsConfigComponent>())
                throw new InvalidOperationException(
                    "DistrictBuildProgressViewSpawnSystem: DistrictBuildProgressViewsConfigComponent world component is missing.");

            if (!_world.Has<VertexGridComponent>())
                throw new InvalidOperationException(
                    "DistrictBuildProgressViewSpawnSystem: VertexGridComponent world component is missing.");

            var viewsConfig = _world.Get<DistrictBuildProgressViewsConfigComponent>().Value;
            var vertexGrid = _world.Get<VertexGridComponent>().Grid;

            var districts = _districts.GetEntities();
            for (var i = 0; i < districts.Length; i++)
            {
                if (districts[i].Get<DistrictBuildStateComponent>().Value != DistrictBuildState.Planned)
                    continue;

                var hexId = districts[i].Get<HexIdFKComponent>();
                if (_viewsByHex.ContainsKey(hexId))
                    continue;

                var districtType = districts[i].Get<DistrictTypeComponent>().Value;
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
                viewEntity.Set(new HexIdFKComponent { Coords = hexId.Coords });
                viewEntity.Set(new DistrictBuildProgressViewComponent { Type = districtType, View = view });
                viewEntity.Set(new DistrictBuildProgressViewTag());
            }
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

        public override void Dispose()
        {
            _districts.Dispose();
            _viewsByHex.Dispose();
            base.Dispose();
        }
    }
}
