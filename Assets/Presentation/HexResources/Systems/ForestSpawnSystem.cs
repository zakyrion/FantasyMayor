using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using Presentation.HexResources.Components;
using Presentation.HexResources.Events;
using Presentation.HexResources.Helpers;
using Presentation.Terrain.Components;
using Unity.Collections;
using UnityEngine;

namespace Presentation.HexResources.Systems
{
    /// <summary>
    ///     Reactive runtime forest spawner. Anchored on the one-frame <see cref="ForestHexAppearedEvent" />
    ///     pulse: on its presence it reconciles state — every forest resource hex that has no view yet gets
    ///     its trees planted (via <see cref="ForestPlanter" />) and its green ground splatted (append-only).
    ///     Works with current world state, not transitive deltas, so it is idempotent: a second pulse in the
    ///     same frame finds nothing missing and no-ops. The startup bulk is done one-shot by
    ///     <see cref="ForestHexResourceViewSubSystem" />; no emitter raises this pulse yet (future gameplay).
    /// </summary>
    [UsedImplicitly]
    public sealed class ForestSpawnSystem : UpdatedSystem
    {
        // After the view systems, well before EventCleanupSystem (int.MaxValue) which disposes the pulse.
        private const int ExecutionPriority = 600;

        // HexResource table indexed by its discriminator value -> the Forest bucket is the wanted set.
        private readonly EntityMultiMap<HexResourcesComponent> _resourcesByType;

        // ResourceView (forest) table indexed by the hex FK -> N tree entities per coordinate.
        private readonly EntityMultiMap<HexIdComponent> _forestViewsByHex;

        private readonly EntitySet _hexSet;
        private readonly ForestPlanter _planter = new();
        private readonly World _world;

        private Transform _root;

        public override int Priority => ExecutionPriority;

        public ForestSpawnSystem(World world)
            : base(world.GetEntities()
                .With<ForestHexAppearedEvent>()
                .AsSet())
        {
            _world = world;
            _resourcesByType = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexResourcesComponent>()
                .AsMultiMap<HexResourcesComponent>();

            _forestViewsByHex = world.GetEntities()
                .With<HexIdComponent>()
                .With<ForestViewComponent>()
                .AsMultiMap<HexIdComponent>();

            _hexSet = world.GetEntities().With<HexTag>().With<HexIdComponent>().AsSet();
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!_world.Has<TerrainTextureComponent>())
                return;

            var texture = _world.Get<TerrainTextureComponent>().Texture;
            if (texture == null)
                return;

            if (!_world.Has<VertexGridComponent>())
                throw new InvalidOperationException(
                    "ForestSpawnSystem: VertexGridComponent world component is missing.");

            if (!_world.Has<TerrainViewConfigComponent>() || !_world.Has<HexResourcesViewConfigComponent>())
                return;

            var forestKey = new HexResourcesComponent { Type = ResourceType.Forest };
            if (!_resourcesByType.TryGetEntities(forestKey, out var forestHexes))
                return;

            var vertexGrid = _world.Get<VertexGridComponent>().Grid;
            var viewConfig = _world.Get<HexResourcesViewConfigComponent>().Value;
            var cellSize = _world.Get<TerrainViewConfigComponent>().CellSize;

            if (_root == null)
                _root = new GameObject("ForestViewRoot").transform;

            var newSplats = new NativeList<ForestGroundPainter.Splat>(64, Allocator.Temp);

            // Forested but not yet viewed -> plant trees and collect their ground splats.
            foreach (ref readonly var hex in forestHexes)
            {
                var hexId = hex.Get<HexIdComponent>();
                if (!_forestViewsByHex.ContainsKey(hexId))
                    _planter.PlantHex(_world, _root, hexId.Coords, vertexGrid, viewConfig, ref newSplats);
            }

            // Append-only: paint just the new patches over the current pixels.
            _planter.Paint(_hexSet, cellSize, newSplats, texture);
            newSplats.Dispose();
        }

        public override void Dispose()
        {
            _resourcesByType.Dispose();
            _forestViewsByHex.Dispose();
            _hexSet.Dispose();
            base.Dispose();
        }
    }
}
