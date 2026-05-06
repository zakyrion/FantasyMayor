using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.AxialSystem;
using Modules.HexesCore.Components;
using Modules.TerrainGenerator.Components;
using Modules.TerrainView.Components;
using Unity.Collections;
using UnityEngine;

namespace Modules.TerrainView.Systems
{
    /// <summary>
    ///     Reacts to <see cref="TerrainGenerationGenerateEventComponent" /> entities, loads the TerrainView prefab via
    ///     <see cref="IAddressable" />, generates the base mesh, runs all <see cref="ViewSubSystem" /> subsystems
    ///     by priority, applies the final heights, and publishes a <see cref="TerrainViewComponent" /> entity.
    ///     Retains ownership of the underlying <see cref="Box{T}" />; disposal destroys the instantiated prefab.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TerrainViewSystem : UpdatedSystem
    {
        internal const int ExecutionPriority = 1000;
        private const string TERRAIN_VIEW_ADDRESS = "TerrainView";

        private readonly IAddressable _addressable;
        private readonly EntitySet _configSet;
        private readonly EntitySet _hexSet;
        private readonly EntitySet _vertexGridSet;
        private readonly IReadOnlyList<ViewSubSystem> _viewSubSystems;
        private readonly World _world;

        private CancellationTokenSource _cts;
        private Box<Views.TerrainView> _terrainViewBox;
        private Entity? _terrainViewEntity;

        /// <inheritdoc />
        public override int Priority => ExecutionPriority;

        /// <param name="world">The ECS world used for entity creation and event subscription.</param>
        /// <param name="addressable">Addressable loader used to load and instantiate the TerrainView prefab.</param>
        /// <param name="viewSubSystems">View subsystems executed after mesh generation, ordered by priority.</param>
        public TerrainViewSystem(World world, IAddressable addressable, IReadOnlyList<ViewSubSystem> viewSubSystems)
            : base(world.GetEntities()
                .WhenAdded<TerrainGenerationGenerateEventComponent>()
                .AsSet())
        {
            _world = world;
            _addressable = addressable;
            _terrainViewBox = Box<Views.TerrainView>.Empty();
            _hexSet = world.GetEntities().With<HexIdComponent>().AsSet();
            _configSet = world.GetEntities().With<TerrainViewConfigComponent>().AsSet();
            _vertexGridSet = world.GetEntities().With<VertexGridComponent>().AsSet();
            _viewSubSystems = viewSubSystems
                .OrderBy(s => s.Priority)
                .ToArray();
        }

        /// <inheritdoc />
        protected override void Update(GameState state, in Entity entity)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            LoadAndSetupAsync(state, CancellationTokenSource.CreateLinkedTokenSource(StatusMonitor.Token, _cts.Token).Token).Forget();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            DestroyTerrainViewEntity();
            DisposeTerrainViewBox();
            _hexSet.Dispose();
            _configSet.Dispose();
            _vertexGridSet.Dispose();

            base.Dispose();
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
        /// <param name="state">Current game state passed to subsystems.</param>
        /// <param name="cancellationToken">Token that aborts the pipeline when the system is re-triggered or disposed.</param>
        private async UniTask LoadAndSetupAsync(GameState state, CancellationToken cancellationToken)
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

            if (_configSet.Count == 0)
            {
                Debug.LogError("[TerrainViewSystem] TerrainViewConfigComponent entity is missing.");
                return;
            }

            var config = _configSet.GetEntities()[0].Get<TerrainViewConfigComponent>();

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

            await RunViewSubSystemsAsync(state, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            if (_vertexGridSet.Count == 0)
            {
                Debug.LogError("[TerrainViewSystem] VertexGridComponent entity is missing.");
                return;
            }

            var vertexGrid = _vertexGridSet.GetEntities()[0].Get<VertexGridComponent>().Grid;
            terrainView.ApplyHeightsFromVertexGrid(vertexGrid);

            DestroyTerrainViewEntity();
            var entity = _world.CreateEntity();
            entity.Set(new TerrainViewComponent { ObjectRef = _terrainViewBox.Value });
            _terrainViewEntity = entity;
        }

        /// <summary>
        ///     Runs all enabled view subsystems in priority order, awaiting each before proceeding.
        /// </summary>
        /// <param name="state">Current game state forwarded to each subsystem.</param>
        /// <param name="cancellationToken">Token that aborts the pipeline.</param>
        private async UniTask RunViewSubSystemsAsync(GameState state, CancellationToken cancellationToken)
        {
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
