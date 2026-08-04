using System;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
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
        // HexResource table indexed by its discriminator value -> the Forest bucket is the wanted set.
        private readonly ComponentIndex<HexResourceComponent, HexResourceType> _resourcesByType;

        // ResourceView (forest) table indexed by the hex FK -> N tree entities per coordinate.
        private readonly ComponentIndex<HexIdFKComponent, HexCoord> _forestViewsByHex;

        private readonly Archetype _hexSet;
        private readonly ForestPlanter _planter = new();
        private readonly EntityStore _world;

        private UnityEngine.Transform _root;

        public override int Priority => SystemPriorities.RuntimeTick.ForestSpawn;

        public ForestSpawnSystem(EntityStore world)
            : base(world, EventArchetypes.Of<ForestHexAppearedEvent>(world))
        {
            _world = world;
            _resourcesByType = world.ComponentIndex<HexResourceComponent, HexResourceType>();
            _forestViewsByHex = world.ComponentIndex<HexIdFKComponent, HexCoord>();
            _hexSet = MapArchetypes.Hex(world);
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            if (!_world.HasWorldComponent<TerrainTextureComponent>())
                return;

            var texture = _world.GetWorldComponent<TerrainTextureComponent>().Texture;
            if (texture == null)
                return;

            if (!_world.HasWorldComponent<VertexGridComponent>())
                throw new InvalidOperationException(
                    "ForestSpawnSystem: VertexGridComponent world component is missing.");

            if (!_world.HasWorldComponent<TerrainViewConfigComponent>() || !_world.HasWorldComponent<HexResourcesViewConfigComponent>())
                return;

            var forestHexes = _resourcesByType[HexResourceType.Forest];
            if (forestHexes.Count == 0)
                return;

            var vertexGrid = _world.GetWorldComponent<VertexGridComponent>().Grid;
            var viewConfig = _world.GetWorldComponent<HexResourcesViewConfigComponent>().Value;
            var cellSize = _world.GetWorldComponent<TerrainViewConfigComponent>().CellSize;

            if (_root == null)
                _root = new GameObject("ForestViewRoot").transform;

            var newSplats = new NativeList<ForestGroundPainter.Splat>(64, Allocator.Temp);

            // Forested but not yet viewed -> plant trees and collect their ground splats.
            foreach (var hex in forestHexes)
            {
                var coords = hex.GetComponent<HexIdFKComponent>().Coords;
                if (_forestViewsByHex[coords].Count == 0)
                    _planter.PlantHex(_world, _root, coords, vertexGrid, viewConfig, ref newSplats);
            }

            // Append-only: paint just the new patches over the current pixels.
            _planter.Paint(_hexSet, cellSize, newSplats, texture);
            newSplats.Dispose();
        }
    }
}
