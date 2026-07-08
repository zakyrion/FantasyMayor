using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Utils;
using Presentation.Terrain.Components;
using Presentation.Terrain.Events;
using Presentation.Terrain.Views;
using Unity.Collections;
using Unity.Mathematics;
using Presentation.Terrain.Tags;
using UnityEngine;

namespace Presentation.Terrain.Systems
{
    /// <summary>
    ///     Synchronizes <see cref="HexSelectedComponent" /> state into the runtime selection view.
    ///     Hides the border when selection disappears and regenerates it when selection changes.
    /// </summary>
    [UsedImplicitly]
    public sealed class HexSelectionViewSystem : UpdatedSystem
    {
        private const int BorderBfsDepth = 3;
        private const float BorderLift = 0.08f;

        private readonly EntitySet _selectedHexSet;
        private readonly EntitySet _viewSet;
        private readonly World _world;

        private bool _hadSelection;
        private HexSelectedComponent _lastSelection;
        private HexSelectionView _lastView;

        /// <inheritdoc />
        public override int Priority => SystemPriorities.RuntimeTick.HexSelectionView;

        public HexSelectionViewSystem(World world)
            : base(world.GetEntities()
                .With<SelectedHexChangedEvent>().With<EventTag>().AsSet())
        {
            _world = world;
            _viewSet = world.GetEntities()
                .With<HexSelectionViewComponent>().With<HexSelectionViewTag>()
                .AsSet();
            _selectedHexSet = world.GetEntities()
                .With<HexSelectedComponent>().With<HexSelectionTag>()
                .AsSet();
        }

        /// <inheritdoc />
        protected override void Update(GameState state, in Entity entity)
        {
            var view = _viewSet.GetEntities()[0].Get<HexSelectionViewComponent>().ObjectRef;

            if (view == null)
                return;

            var viewChanged = _lastView != view;

            if (_selectedHexSet.Count == 0)
            {
                if (_hadSelection || viewChanged)
                    view.HideSelectionMesh();

                _hadSelection = false;
                _lastView = view;
                return;
            }

            if (!_world.Has<VertexGridComponent>())
                throw new InvalidOperationException("HexSelectionViewSystem: VertexGridComponent world component is missing.");

            var selected = _selectedHexSet.GetEntities()[0].Get<HexSelectedComponent>();
            if (!viewChanged && _hadSelection && _lastSelection.Coords == selected.Coords)
            {
                return;
            }

            VertexGrid vertexGrid = _world.Get<VertexGridComponent>().Grid;
            ComputeSelectionRings(selected.Coords, vertexGrid, out var outerRing, out var innerRing);

            view.ShowSelectionBorder(outerRing, innerRing);

            outerRing.Dispose();
            innerRing.Dispose();

            _hadSelection = true;
            _lastSelection = selected;
            _lastView = view;
        }

        private void ComputeSelectionRings(
            HexCoord selectedHex,
            VertexGrid grid,
            out NativeArray<float3> outerRing,
            out NativeArray<float3> innerRing)
        {
            var allOwned = new NativeHashSet<VertexCoord>(64, Allocator.Temp);
            foreach (var coord in grid.GetOwnedVertexCoords(selectedHex))
                allOwned.Add(coord);

            var outerList = new NativeList<VertexCoord>(Allocator.Temp);
            Span<VertexCoord> neighborBuffer = stackalloc VertexCoord[6];

            foreach (var coord in allOwned)
            {
                var vertex = grid.Get(coord);
                if (vertex.OwnerCount > 1)
                {
                    outerList.Add(coord);
                    continue;
                }

                var neighborCount = grid.GetNeighbors(coord, neighborBuffer);
                if (neighborCount < AxialMath.NeighborCount)
                    outerList.Add(coord);
            }

            var visited = new NativeHashSet<VertexCoord>(64, Allocator.Temp);
            var current = new NativeList<VertexCoord>(Allocator.Temp);
            var next = new NativeList<VertexCoord>(Allocator.Temp);

            for (var i = 0; i < outerList.Length; i++)
            {
                visited.Add(outerList[i]);
                current.Add(outerList[i]);
            }

            for (var step = 0; step < BorderBfsDepth; step++)
            {
                for (var c = 0; c < current.Length; c++)
                {
                    var count = grid.GetNeighbors(current[c], neighborBuffer);
                    for (var d = 0; d < count; d++)
                    {
                        var neighbor = neighborBuffer[d];
                        if (allOwned.Contains(neighbor) && visited.Add(neighbor))
                            next.Add(neighbor);
                    }
                }

                (current, next) = (next, current);
                next.Clear();
            }

            outerRing = new NativeArray<float3>(outerList.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < outerList.Length; i++)
            {
                var pos = grid.Get(outerList[i]).Position;
                outerRing[i] = new float3(pos.x, pos.y + BorderLift, pos.z);
            }

            innerRing = new NativeArray<float3>(current.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < current.Length; i++)
            {
                var pos = grid.Get(current[i]).Position;
                innerRing[i] = new float3(pos.x, pos.y + BorderLift, pos.z);
            }

            allOwned.Dispose();
            outerList.Dispose();
            visited.Dispose();
            current.Dispose();
            next.Dispose();
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _selectedHexSet.Dispose();
            base.Dispose();
        }
    }
}
