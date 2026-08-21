using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.AxialSystem;
using Modules.Boot.Core;
using Presentation.Archetypes;
using Presentation.Terrain.Components;
using Unity.Collections;
using UnityEngine;

namespace Presentation.Terrain.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 300). Loads the TerrainView prefab via <see cref="IAddressable" />,
    ///     generates the base mesh, runs all <see cref="ViewSubSystem" /> subsystems by priority, applies the
    ///     final heights, and publishes a <see cref="TerrainViewComponent" /> entity.
    ///     Retains ownership of the underlying <see cref="Box{T}" />; disposal destroys the instantiated prefab.
    ///     Cancellation is owned by the orchestrator and flows in through the update token.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TerrainViewSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const string TERRAIN_VIEW_ADDRESS = "TerrainView";

        private readonly IAddressable _addressable;
        private readonly Archetype _hexSet;
        private readonly IReadOnlyList<ViewSubSystem> _viewSubSystems;
        private readonly EntityStorages _storages;
        private readonly Archetype _terrainViewArchetype;

        private Box<Views.TerrainView> _terrainViewBox;
        private Entity? _terrainViewEntity;

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.TerrainView;

        /// <param name="storages">Named ECS storages used for game-world entity creation.</param>
        /// <param name="addressable">Addressable loader used to load and instantiate the TerrainView prefab.</param>
        /// <param name="viewSubSystems">View subsystems executed after mesh generation, ordered by priority.</param>
        public TerrainViewSystem(EntityStorages storages, IAddressable addressable, IReadOnlyList<ViewSubSystem> viewSubSystems)
        {
            _storages = storages;
            _addressable = addressable;
            _terrainViewBox = Box<Views.TerrainView>.Empty();
            _hexSet = MapArchetypes.Hex(storages.World);
            _terrainViewArchetype = PresentationArchetypes.TerrainView(storages.World);
            _viewSubSystems = viewSubSystems
                .OrderBy(s => s.Priority)
                .ToArray();
        }

        /// <inheritdoc />
        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            return LoadAndSetupAsync(cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            DestroyTerrainViewEntity();
            DisposeTerrainViewBox();
        }

        /// <summary>
        ///     Allocates a <see cref="NativeHashSet{T}" /> and fills it with <see cref="HexCoord" /> values
        ///     from all entities carrying a <see cref="HexIdComponent" />.
        ///     Caller is responsible for disposing the returned set.
        /// </summary>
        private NativeHashSet<HexCoord> CollectHexCoords()
        {
            var entities = _hexSet.Entities;
            var hexCoords = new NativeHashSet<HexCoord>(entities.Count, Allocator.TempJob);

            foreach (var entity in entities)
                hexCoords.Add(entity.GetComponent<HexIdComponent>().Coords);

            return hexCoords;
        }

        /// <summary>
        ///     Destroys the previously created <see cref="TerrainViewComponent" /> entity if it exists.
        /// </summary>
        private void DestroyTerrainViewEntity()
        {
            if (_terrainViewEntity == null || _terrainViewEntity.Value.IsNull)
                return;

            _terrainViewEntity.Value.DeleteEntity();
            _terrainViewEntity = null;
        }

        private void DisposeTerrainViewBox()
        {
            if (!_terrainViewBox.Exist)
                return;

            _terrainViewBox.Dispose();
            _terrainViewBox = Box<Views.TerrainView>.Empty();
        }

        /// <summary>
        ///     Loads and instantiates the TerrainView prefab, generates the base mesh, runs all
        ///     view subsystems by priority, applies final heights, and creates the result entity.
        /// </summary>
        /// <param name="cancellationToken">Token that aborts the pipeline when the orchestrator is disposed.</param>
        private async UniTask LoadAndSetupAsync(CancellationToken cancellationToken)
        {
            DisposeTerrainViewBox();

            var result = await _addressable.LoadAndInstanceAsync<Views.TerrainView>(TERRAIN_VIEW_ADDRESS, cancellationToken);

            if (cancellationToken.IsCancellationRequested || result.Status != Status.Success)
            {
                if (result.Status == Status.Success)
                    result.Box.Dispose();

                return;
            }

            _terrainViewBox = result.Box;
            var terrainView = _terrainViewBox.Value;

            if (!_storages.Singletons.Has<TerrainViewConfigComponent>())
            {
                Debug.LogError("[TerrainViewSystem] TerrainViewConfigComponent is missing.");
                return;
            }

            var config = _storages.Singletons.Get<TerrainViewConfigComponent>();

            var hexCoords = CollectHexCoords();
            try
            {
                terrainView.Generate(hexCoords, config.CellSize, config.Subdivisions);
            }
            finally
            {
                hexCoords.Dispose();
            }

            if (cancellationToken.IsCancellationRequested)
                return;

            await RunViewSubSystemsAsync(cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            ApplyGeneratedTexture(terrainView);

            if (!_storages.Singletons.Has<VertexGridComponent>())
                throw new InvalidOperationException("TerrainViewSystem: VertexGridComponent singleton component is missing.");

            var vertexGrid = _storages.Singletons.Get<VertexGridComponent>().Grid;
            terrainView.ApplyHeightsFromVertexGrid(vertexGrid);

            DestroyTerrainViewEntity();
            var entity = _terrainViewArchetype.CreateEntity();
            entity.AddComponent(new TerrainViewComponent { ObjectRef = _terrainViewBox.Value });
            _terrainViewEntity = entity;
        }

        /// <summary>
        ///     Reads the <see cref="TerrainTextureComponent" /> singleton component created by the texture
        ///     subsystem and applies the texture to the terrain view material.
        ///     The singleton component persists: the same <see cref="UnityEngine.Texture2D" /> instance
        ///     stays assigned to the material, so reactive runtime systems (e.g. forest ground painting)
        ///     can mutate its pixels and have the material reflect the change without re-applying.
        /// </summary>
        /// <param name="terrainView">Target terrain view that receives the texture.</param>
        private void ApplyGeneratedTexture(Views.TerrainView terrainView)
        {
            if (!_storages.Singletons.Has<TerrainTextureComponent>())
                return;

            var texture = _storages.Singletons.Get<TerrainTextureComponent>().Texture;
            terrainView.ApplyTexture(texture);
        }

        /// <summary>
        ///     Runs all enabled view subsystems in priority order, awaiting each before proceeding.
        /// </summary>
        /// <param name="cancellationToken">Token that aborts the pipeline.</param>
        private async UniTask RunViewSubSystemsAsync(CancellationToken cancellationToken)
        {
            // View building is one-shot; deltaTime is irrelevant, so a default GameState is passed through.
            var state = default(GameState);

            foreach (var subSystem in _viewSubSystems)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (!subSystem.IsEnabled)
                    continue;

                await subSystem.Update(state, cancellationToken);
            }
        }
    }
}
