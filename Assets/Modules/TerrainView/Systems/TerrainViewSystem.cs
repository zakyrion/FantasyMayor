using System;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.AxialSystem;
using Modules.Boot.Core;
using Modules.TerrainView.Components;
using System.Collections.Generic;
using System.Linq;
using Modules.HexCore.Components;
using Unity.Collections;
using UnityEngine;

namespace Modules.TerrainView.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 300). Loads the TerrainView prefab via <see cref="IAddressable" />,
    ///     generates the base mesh, runs all <see cref="ViewSubSystem" /> subsystems by priority, applies the
    ///     final heights, and publishes a <see cref="TerrainViewComponent" /> entity.
    ///     Retains ownership of the underlying <see cref="Box{T}" />; disposal destroys the instantiated prefab.
    ///     Cancellation is owned by the orchestrator and flows in through the update token.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TerrainViewSystem : IPrioritizedUniTaskSystem<TerrainGenerationStep>
    {
        private const int ExecutionPriority = 300;
        private const string TERRAIN_VIEW_ADDRESS = "TerrainView";

        private readonly IAddressable _addressable;
        private readonly EntitySet _hexSet;
        private readonly IReadOnlyList<ViewSubSystem> _viewSubSystems;
        private readonly World _world;

        private Box<Views.TerrainView> _terrainViewBox;
        private Entity? _terrainViewEntity;

        /// <inheritdoc />
        public int Priority => ExecutionPriority;

        /// <param name="world">The ECS world used for entity creation.</param>
        /// <param name="addressable">Addressable loader used to load and instantiate the TerrainView prefab.</param>
        /// <param name="viewSubSystems">View subsystems executed after mesh generation, ordered by priority.</param>
        public TerrainViewSystem(World world, IAddressable addressable, IReadOnlyList<ViewSubSystem> viewSubSystems)
        {
            _world = world;
            _addressable = addressable;
            _terrainViewBox = Box<Views.TerrainView>.Empty();
            _hexSet = world.GetEntities().With<HexIdComponent>().AsSet();
            _viewSubSystems = viewSubSystems
                .OrderBy(s => s.Priority)
                .ToArray();
        }

        /// <inheritdoc />
        public UniTask Update(TerrainGenerationStep state, CancellationToken cancellationToken)
        {
            return LoadAndSetupAsync(cancellationToken);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            DestroyTerrainViewEntity();
            DisposeTerrainViewBox();
            _hexSet.Dispose();
        }

        /// <summary>
        ///     Allocates a <see cref="NativeHashSet{T}" /> and fills it with <see cref="HexCoord" /> values
        ///     from all entities carrying a <see cref="HexIdComponent" />.
        ///     Caller is responsible for disposing the returned set.
        /// </summary>
        private NativeHashSet<HexCoord> CollectHexCoords()
        {
            var entities = _hexSet.GetEntities();
            var hexCoords = new NativeHashSet<HexCoord>(entities.Length, Allocator.TempJob);

            foreach (ref readonly var entity in entities)
                hexCoords.Add(entity.Get<HexIdComponent>().Coords);

            return hexCoords;
        }

        /// <summary>
        ///     Destroys the previously created <see cref="TerrainViewComponent" /> entity if it exists.
        /// </summary>
        private void DestroyTerrainViewEntity()
        {
            if (_terrainViewEntity == null || !_terrainViewEntity.Value.IsAlive)
                return;

            _terrainViewEntity.Value.Dispose();
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

            if (!_world.Has<TerrainViewConfigComponent>())
            {
                Debug.LogError("[TerrainViewSystem] TerrainViewConfigComponent is missing.");
                return;
            }

            var config = _world.Get<TerrainViewConfigComponent>();

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

            if (!_world.Has<VertexGridComponent>())
                throw new InvalidOperationException("TerrainViewSystem: VertexGridComponent world component is missing.");

            var vertexGrid = _world.Get<VertexGridComponent>().Grid;
            terrainView.ApplyHeightsFromVertexGrid(vertexGrid);

            DestroyTerrainViewEntity();
            var entity = _world.CreateEntity();
            entity.Set(new TerrainViewComponent { ObjectRef = _terrainViewBox.Value });
            _terrainViewEntity = entity;
        }

        /// <summary>
        ///     Reads the <see cref="TerrainTextureComponent" /> world component created by the texture
        ///     subsystem and applies the texture to the terrain view material.
        ///     The world component persists: the same <see cref="UnityEngine.Texture2D" /> instance
        ///     stays assigned to the material, so reactive runtime systems (e.g. forest ground painting)
        ///     can mutate its pixels and have the material reflect the change without re-applying.
        /// </summary>
        /// <param name="terrainView">Target terrain view that receives the texture.</param>
        private void ApplyGeneratedTexture(Views.TerrainView terrainView)
        {
            if (!_world.Has<TerrainTextureComponent>())
                return;

            var texture = _world.Get<TerrainTextureComponent>().Texture;
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
