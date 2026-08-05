using System;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Archetypes;
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

        private readonly Archetype _forestViews;
        private readonly Archetype _hexSet;
        private readonly ForestPlanter _planter = new();
        private readonly EntityStorages _storages;

        private UnityEngine.Transform _root;

        public override int Priority => SystemPriorities.RuntimeTick.ForestSpawn;

        public ForestSpawnSystem(EntityStorages storages)
            : base(storages.World, EventArchetypes.Of<ForestHexAppearedEvent>(storages.World))
        {
            _storages = storages;
            _resourcesByType = storages.World.ComponentIndex<HexResourceComponent, HexResourceType>();
            _forestViews = PresentationArchetypes.ForestView(storages.World);
            _hexSet = MapArchetypes.Hex(storages.World);
        }

        // The pulse entity itself is ignored — reconciliation is global over current state.
        protected override void Update(GameState state, in Entity pulse)
        {
            if (!EcsEventExtensions.IsRipe(pulse))
                return;

            if (!_storages.World.HasWorldComponent<TerrainTextureComponent>())
                return;

            var texture = _storages.World.GetWorldComponent<TerrainTextureComponent>().Texture;
            if (texture == null)
                return;

            if (!_storages.World.HasWorldComponent<VertexGridComponent>())
                throw new InvalidOperationException(
                    "ForestSpawnSystem: VertexGridComponent world component is missing.");

            if (!_storages.World.HasWorldComponent<TerrainViewConfigComponent>() || !_storages.World.HasWorldComponent<HexResourcesViewConfigComponent>())
                return;

            var forestResources = _resourcesByType[HexResourceType.Forest];
            if (forestResources.Count == 0)
                return;

            var vertexGrid = _storages.World.GetWorldComponent<VertexGridComponent>().Grid;
            var viewConfig = _storages.World.GetWorldComponent<HexResourcesViewConfigComponent>().Value;
            var cellSize = _storages.World.GetWorldComponent<TerrainViewConfigComponent>().CellSize;

            if (_root == null)
                _root = new GameObject("ForestViewRoot").transform;

            var newSplats = new NativeList<ForestGroundPainter.Splat>(64, Allocator.Temp);
            var forestHexes = new NativeHashSet<HexCoord>(Math.Max(1, forestResources.Count), Allocator.Temp);
            var viewedHexes = new NativeHashSet<HexCoord>(Math.Max(1, _forestViews.Count), Allocator.Temp);

            try
            {
                // Snapshot both table scopes before PlantHex structurally creates ForestView entities.
                foreach (var resource in forestResources)
                    forestHexes.Add(resource.GetComponent<HexIdFKComponent>().Coords);

                foreach (var view in _forestViews.Entities)
                    viewedHexes.Add(view.GetComponent<HexIdFKComponent>().Coords);

                // Forested but not yet viewed -> plant trees and collect their ground splats.
                foreach (var coords in forestHexes)
                {
                    if (!viewedHexes.Contains(coords))
                        _planter.PlantHex(_storages.World, _root, coords, vertexGrid, viewConfig, ref newSplats);
                }

                // Append-only: paint just the new patches over the current pixels.
                _planter.Paint(_hexSet, cellSize, newSplats, texture);
            }
            finally
            {
                viewedHexes.Dispose();
                forestHexes.Dispose();
                newSplats.Dispose();
            }
        }
    }
}
