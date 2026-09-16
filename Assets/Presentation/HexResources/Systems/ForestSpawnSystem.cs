using System;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.Boot.Core;
using Presentation.Archetypes;
using Presentation.HexResources.Configs;
using Presentation.HexResources.Events;
using Presentation.HexResources.Helpers;
using Presentation.Terrain.Components;
using Presentation.Terrain.Configs;
using Unity.Collections;
using UnityEngine;
using Transform = UnityEngine.Transform;

namespace Presentation.HexResources.Systems
{
    /// <summary>
    ///     Reactive runtime forest spawner. Reads the <see cref="ForestHexAppearedEvent" /> log event: on every
    ///     event it reconciles state — every forest resource hex that has no view yet gets its trees planted (via
    ///     <see cref="ForestPlanter" />) and its green ground splatted (append-only). Works with current world
    ///     state, not the event payload, so it is idempotent: a second event in the same tick finds nothing
    ///     missing and no-ops. The startup bulk is done one-shot by <see cref="ForestHexResourceViewSubSystem" />;
    ///     no producer raises this event yet (future gameplay) — a dormant consumer (event/dormant-consumer).
    /// </summary>
    [UsedImplicitly]
    public sealed class ForestSpawnSystem : IUpdatedSystem
    {
        private readonly EventReader<ForestHexAppearedEvent> _forestHexAppearances;

        private readonly Archetype _forestViews;
        private readonly Archetype _hexSet;
        private readonly ForestPlanter _planter = new();
        // HexResource table indexed by its discriminator value -> the Forest bucket is the wanted set.
        private readonly ComponentIndex<HexResourceComponent, HexResourceType> _resourcesByType;
        private readonly EntityStorages _storages;

        private Transform _root;

        public AppState AppState { get; }
        public int Priority => SystemPriorities.RuntimeTick.ForestSpawn;

        public ForestSpawnSystem(AppState appState, EntityStorages storages, EventReader<ForestHexAppearedEvent> forestHexAppearances)
        {
            AppState = appState;
            _storages = storages;
            _forestHexAppearances = forestHexAppearances;
            _resourcesByType = storages.World.ComponentIndex<HexResourceComponent, HexResourceType>();
            _forestViews = PresentationArchetypes.ForestView(storages.World);
            _hexSet = MapArchetypes.Hex(storages.World);
        }

        public void Update(GameState state)
        {
            while (_forestHexAppearances.TryRead(out _))
                SpawnMissingForests();
        }

        // Reconciliation is global over current state — the event's payload itself is ignored.
        private void SpawnMissingForests()
        {
            if (!_storages.Singletons.Has<TerrainTextureComponent>())
                return;

            var texture = _storages.Singletons.Get<TerrainTextureComponent>().Texture;
            if (texture == null)
                return;

            if (!_storages.Singletons.Has<VertexGridComponent>())
                throw new InvalidOperationException(
                    "ForestSpawnSystem: VertexGridComponent singleton component is missing.");

            var forestResources = _resourcesByType[HexResourceType.Forest];
            if (forestResources.Count == 0)
                return;

            var vertexGrid = _storages.Singletons.Get<VertexGridComponent>().Grid;
            var viewConfig = _storages.Get<HexResourcesViewConfig>();
            var cellSize = _storages.Get<TerrainViewConfig>().CellSize;

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
