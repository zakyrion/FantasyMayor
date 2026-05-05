using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Modules.AxialSystem
{
    /// <summary>
    ///     Generic sparse axial-coordinate grid backed by a Dictionary.
    ///     Infinite — no pre-defined bounds required. _offset tracks the min coord
    ///     seen so far, allowing world-space bounds queries via AxialToWorld(_offset).
    /// </summary>
    public abstract class AxialGrid<TCoord, T> where TCoord : struct, IAxialCoord<TCoord>
    {
        private readonly Dictionary<TCoord, T> _cells = new();

        /// <summary>All populated cells as coord/value pairs.</summary>
        public IEnumerable<KeyValuePair<TCoord, T>> All => _cells;

        /// <summary>World-space distance between adjacent axial steps.</summary>
        public float CellSize { get; private set; }

        public IEnumerable<TCoord> Coords => _cells.Keys;

        public int Count => _cells.Count;

        /// <summary>
        ///     Max axial coord among all Set cells (updated on each Set call).
        /// </summary>
        public int2 MaxCoord { get; private set; } = new(int.MinValue, int.MinValue);

        /// <summary>
        ///     Min axial coord among all Set cells (updated on each Set call).
        ///     Use AxialToWorld(_offset) to get the world-space lower-left corner.
        /// </summary>
        public int2 MinCoord { get; private set; } = new(int.MaxValue, int.MaxValue);

        public AxialOrientation Orientation { get; }

        protected AxialGrid(float cellSize = 1f, AxialOrientation orientation = AxialOrientation.PointyTop)
        {
            CellSize = cellSize;
            Orientation = orientation;
        }

        protected abstract TCoord CreateCoord(int2 value);

        public float3 AxialToWorld(TCoord coord)
        {
            return Orientation == AxialOrientation.PointyTop
                ? AxialMath.AxialToWorldPointTop(coord.Value, CellSize)
                : AxialMath.AxialToWorldFlatTop(coord.Value, CellSize);
        }

        public bool Contains(TCoord coord)
        {
            return _cells.ContainsKey(coord);
        }

        public T Get(TCoord coord)
        {
            return _cells[coord];
        }

        // -------------------------------------------------------------------------
        // Neighbor lookup
        // -------------------------------------------------------------------------

        /// <summary>
        ///     Fill <paramref name="result" /> with axial coords of existing neighbors.
        ///     Returns the count of valid neighbors found (max 6).
        /// </summary>
        public int GetNeighbors(TCoord coord, Span<TCoord> result)
        {
            var count = 0;
            for (var d = 0; d < AxialMath.NeighborCount; d++)
            {
                var neighbor = CreateCoord(coord.Value + AxialMath.NeighborDirs[d]);
                if (_cells.ContainsKey(neighbor))
                    result[count++] = neighbor;
            }
            return count;
        }

        public virtual void Clear()
        {
            _cells.Clear();
            MinCoord = new int2(int.MaxValue, int.MaxValue);
            MaxCoord = new int2(int.MinValue, int.MinValue);
        }

        public virtual void Remove(TCoord coord)
        {
            _cells.Remove(coord);
        }

        // -------------------------------------------------------------------------
        // Core operations
        // -------------------------------------------------------------------------

        public virtual void Set(TCoord coord, T value)
        {
            _cells[coord] = value;

            var raw = coord.Value;
            if (raw.x < MinCoord.x || raw.y < MinCoord.y)
                MinCoord = new int2(math.min(MinCoord.x, raw.x), math.min(MinCoord.y, raw.y));
            if (raw.x > MaxCoord.x || raw.y > MaxCoord.y)
                MaxCoord = new int2(math.max(MaxCoord.x, raw.x), math.max(MaxCoord.y, raw.y));
        }

        /// <summary>Update CellSize after construction (needed when the grid is pre-created as a VContainer singleton).</summary>
        public void SetCellSize(float cellSize)
        {
            CellSize = cellSize;
        }

        public bool TryGet(TCoord coord, out T value)
        {
            return _cells.TryGetValue(coord, out value);
        }

        // -------------------------------------------------------------------------
        // World ↔ Axial convenience wrappers
        // -------------------------------------------------------------------------

        public TCoord WorldToAxial(float3 worldPos)
        {
            var raw = Orientation == AxialOrientation.PointyTop
                ? AxialMath.WorldToAxial(worldPos, CellSize)
                : AxialMath.WorldToAxialFlatTop(worldPos, CellSize);
            return CreateCoord(raw);
        }
    }
}
