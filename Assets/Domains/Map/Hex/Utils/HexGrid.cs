using System;
using System.Collections.Generic;
using Modules.AxialSystem;
using Domains.Map.Hex.Data;
using Unity.Collections;
using Unity.Mathematics;

namespace Domains.Map.Hex.Utils
{
    /// <summary>
    ///     Fine vertex grid singleton — one cell per subdivided vertex.
    ///     Uses flat-top axial orientation: the dual of pointy-top tiles, so that BFS expansion
    ///     produces a pointy-top shaped region matching the actual coarse hex tiles.
    ///     Constructed at terrain load and published as the VertexGridComponent world component.
    /// </summary>
    public class VertexGrid : AxialGrid<VertexCoord, HexVertex>
    {
        private readonly float _cellSize;
        private readonly Dictionary<HexCoord, HashSet<VertexCoord>> _ownerVertexCoords = new();
        private readonly int _subdivisions;

        public VertexGrid(float cellSize, int subdivisions) : base(orientation: AxialOrientation.FlatTop)
        {
            _cellSize = cellSize;
            _subdivisions = subdivisions;
        }

        public override void Clear()
        {
            base.Clear();
            _ownerVertexCoords.Clear();
        }

        public override void Remove(VertexCoord coord)
        {
            if (TryGet(coord, out var existing))
                RemoveFromOwnerCache(coord, existing);

            base.Remove(coord);
        }

        public override void Set(VertexCoord coord, HexVertex value)
        {
            if (TryGet(coord, out var existing))
                RemoveFromOwnerCache(coord, existing);

            base.Set(coord, value);
            AddToOwnerCache(coord, value);
        }

        /// <summary>
        ///     Clears all vertices and rebuilds from current HexGrid state.
        ///     Each hex expands via BFS (waveCount = subdivisions waves) from its fine-grid center.
        ///     Boundary vertices naturally accumulate multiple owners from adjacent hexes.
        /// </summary>
        public void BuildVertices(NativeArray<HexCoord> grid)
        {
            SetCellSize(AxialMath.VertexCellSize(_cellSize, _subdivisions));
            Clear();

            var visited = new HashSet<VertexCoord>();
            var current = new List<VertexCoord>();
            var next = new List<VertexCoord>();

            foreach (var hex in grid)
            {
                var fineCenter = WorldToAxial(AxialMath.AxialToWorldPointTop(hex.Value, _cellSize));

                visited.Clear();
                current.Clear();
                next.Clear();

                current.Add(fineCenter);
                visited.Add(fineCenter);

                for (var wave = 0; wave <= _subdivisions; wave++)
                {
                    foreach (var coord in current)
                    {
                        if (TryGet(coord, out var vertex))
                        {
                            if (!vertex.OwnedBy(hex) && vertex.OwnerCount < 3)
                                vertex.AddOwner(hex);
                            Set(coord, vertex);
                        }
                        else
                        {
                            vertex = new HexVertex { Position = AxialToWorld(coord), MeshIndex = -1 };
                            vertex.AddOwner(hex);
                            Set(coord, vertex);
                        }

                        if (wave == _subdivisions)
                            continue;

                        for (var d = 0; d < AxialMath.NeighborCount; d++)
                        {
                            var neighbor = coord + AxialMath.NeighborsPointyTop[d];
                            if (visited.Add(neighbor))
                                next.Add(neighbor);
                        }
                    }

                    (current, next) = (next, current);
                    next.Clear();
                }
            }
        }

        public VertexCoord GetCenterVertexCoord(HexCoord hexCoord)
        {
            return WorldToAxial(AxialMath.AxialToWorldPointTop(hexCoord.Value, _cellSize));
        }

        // Returns the LIVE owner-cache set, not a copy. Set() re-runs the cache bookkeeping (remove+re-add),
        // so calling Set inside a foreach over this throws "Collection was modified" — snapshot the coords
        // first (e.g. into a NativeList) and iterate the snapshot (see ClayDepressionShaper / forest spawn).
        public IEnumerable<VertexCoord> GetOwnedVertexCoords(HexCoord hexCoord)
        {
            return _ownerVertexCoords.TryGetValue(hexCoord, out var coords)
                ? coords
                : Array.Empty<VertexCoord>();
        }

        protected override VertexCoord CreateCoord(int2 value)
        {
            return new VertexCoord(value);
        }

        private void AddToOwnerCache(VertexCoord coord, in HexVertex vertex)
        {
            for (var i = 0; i < vertex.OwnerCount; i++)
            {
                var owner = vertex[i];
                if (!_ownerVertexCoords.TryGetValue(owner, out var coords))
                {
                    coords = new HashSet<VertexCoord>();
                    _ownerVertexCoords.Add(owner, coords);
                }

                coords.Add(coord);
            }
        }

        private void RemoveFromOwnerCache(VertexCoord coord, in HexVertex vertex)
        {
            for (var i = 0; i < vertex.OwnerCount; i++)
            {
                var owner = vertex[i];
                if (!_ownerVertexCoords.TryGetValue(owner, out var coords))
                    continue;

                coords.Remove(coord);
                if (coords.Count == 0)
                    _ownerVertexCoords.Remove(owner);
            }
        }
    }
}
