using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Map.Hex.Components;
using JetBrains.Annotations;
using Presentation.Districts.Components;
using Presentation.Districts.Configs;
using Presentation.Districts.Views;
using Presentation.Terrain.Components;
using UnityEngine;
using Object = UnityEngine.Object;
using Presentation.Districts.Tags;

namespace Presentation.Districts.Systems
{
    /// <summary>
    ///     Reactive runtime district-view spawner. Anchored on the one-frame <see cref="DistrictBuiltEvent" />
    ///     pulse: on its presence it reconciles state — every committed district entity
    ///     (<see cref="BuildDistrictActionTag" />) that has no view yet gets its prefab instantiated on the hex
    ///     centre. Works with current world state, not the pulse payload, so it is idempotent: a second pulse in
    ///     the same frame finds nothing missing and no-ops. The pulse is raised by
    ///     <c>BuildDistrictActionSystem</c> when a build is committed.
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictViewSpawnSystem : UpdatedSystem
    {
        // Committed district entities: one per built hex, carrying the hex FK and its district type.
        private readonly EntitySet _districts;

        // District view entities indexed by the hex FK -> lets the reconcile skip hexes already viewed.
        private readonly EntityMultiMap<HexIdComponent> _viewsByHex;

        private readonly World _world;

        private Transform _root;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictViewSpawn;

        public DistrictViewSpawnSystem(World world)
            : base(world.GetEntities()
                .With<DistrictBuiltEvent>()
                .AsSet())
        {
            _world = world;

            _districts = world.GetEntities()
                .With<BuildDistrictActionTag>()
                .With<HexIdComponent>()
                .With<DistrictTypeComponent>()
                .AsSet();

            _viewsByHex = world.GetEntities()
                .With<HexIdComponent>()
                .With<DistrictViewComponent>().With<DistrictViewTag>()
                .AsMultiMap<HexIdComponent>();
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!_world.Has<DistrictViewsConfigComponent>())
                throw new InvalidOperationException(
                    "DistrictViewSpawnSystem: DistrictViewsConfigComponent world component is missing.");

            if (!_world.Has<VertexGridComponent>())
                throw new InvalidOperationException(
                    "DistrictViewSpawnSystem: VertexGridComponent world component is missing.");

            var viewsConfig = _world.Get<DistrictViewsConfigComponent>().Value;
            var vertexGrid = _world.Get<VertexGridComponent>().Grid;

            var districts = _districts.GetEntities();
            for (var i = 0; i < districts.Length; i++)
            {
                var hexId = districts[i].Get<HexIdComponent>();
                if (_viewsByHex.ContainsKey(hexId))
                    continue;

                var districtType = districts[i].Get<DistrictTypeComponent>().Value;
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

                var viewEntity = _world.CreateEntity();
                viewEntity.Set(new HexIdComponent { Coords = hexId.Coords });
                viewEntity.Set(new DistrictViewComponent { Type = districtType, View = view });
                viewEntity.Set(new DistrictViewTag());
            }
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

        public override void Dispose()
        {
            _districts.Dispose();
            _viewsByHex.Dispose();
            base.Dispose();
        }
    }
}
