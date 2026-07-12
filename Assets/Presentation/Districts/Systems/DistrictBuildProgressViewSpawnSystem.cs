using System;
using DefaultEcs;
using DefaultECSExtensions;
using Domains.Actions.BuildDistrictAction.Components;
using Domains.Actions.BuildDistrictAction.Events;
using Domains.Economy.District.Components;
using Domains.Economy.District.Data;
using Domains.Economy.District.Helpers;
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
    ///     <see cref="DistrictBuildConfirmedEvent" /> pulse (the same pulse <c>BuildDistrictActionSystem</c>
    ///     consumes to create the in-progress entity — this system runs right after it, same tick): on its
    ///     presence it reconciles state — every in-progress build (<c>BuildDistrictInProgressTag</c>) whose hex
    ///     has no progress view yet gets its prefab instantiated on the hex centre. Works with current world
    ///     state, not the pulse payload, so it is idempotent. Torn down by
    ///     <see cref="DistrictBuildProgressViewDespawnSystem" /> at build completion (R1) or cancel (R5).
    /// </summary>
    [UsedImplicitly]
    public sealed class DistrictBuildProgressViewSpawnSystem : UpdatedSystem
    {
        // In-progress build entities: one per hex under construction, carrying the hex FK and its district type.
        private readonly EntitySet _inProgress;

        // Progress-view entities indexed by the hex FK -> lets the reconcile skip hexes already viewed.
        private readonly EntityMultiMap<HexIdComponent> _viewsByHex;

        private readonly World _world;

        private Transform _root;

        public override int Priority => SystemPriorities.RuntimeTick.DistrictBuildProgressViewSpawn;

        public DistrictBuildProgressViewSpawnSystem(World world)
            : base(world.GetEntities()
                .With<DistrictBuildConfirmedEvent>()
                .AsSet())
        {
            _world = world;

            _inProgress = world.GetEntities()
                .With<BuildDistrictInProgressTag>()
                .With<HexIdComponent>()
                .With<DistrictTypeComponent>()
                .AsSet();

            _viewsByHex = world.GetEntities()
                .With<HexIdComponent>()
                .With<DistrictBuildProgressViewComponent>().With<DistrictBuildProgressViewTag>()
                .AsMultiMap<HexIdComponent>();
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

            var inProgress = _inProgress.GetEntities();
            for (var i = 0; i < inProgress.Length; i++)
            {
                var hexId = inProgress[i].Get<HexIdComponent>();
                if (_viewsByHex.ContainsKey(hexId))
                    continue;

                var districtType = inProgress[i].Get<DistrictTypeComponent>().Value;
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
                viewEntity.Set(new HexIdComponent { Coords = hexId.Coords });
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
            _inProgress.Dispose();
            _viewsByHex.Dispose();
            base.Dispose();
        }
    }
}
