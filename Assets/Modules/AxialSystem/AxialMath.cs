using Unity.Mathematics;

/// <summary>
///     Hex grid orientation. Determines which world ↔ axial formulas are used.
///     PointyTop — coarse HexGrid (tiles have top/bottom vertices).
///     FlatTop   — fine VertexGrid (dual of pointy-top; BFS ball = pointy-top shape in world).
/// </summary>
public enum AxialOrientation { PointyTop, FlatTop }

/// <summary>
/// Single source of truth for all axial hex coordinate math.
/// Works at any scale: pass hexSize for coarse hex grid, hexSize/subdivisions for fine vertex grid.
/// Contains both pointy-top (coarse HexGrid) and flat-top (fine VertexGrid) variants.
/// The two orientations are geometric duals: the axial distance ball of one appears as the
/// opposite orientation in world space, which is why VertexGrid (interior of pointy-top tiles)
/// must use flat-top coordinates to produce pointy-top shaped coverage.
/// </summary>
public static class AxialMath
{
    /// <summary>
    /// The 6 axial neighbor directions for pointy-top orientation (coarse HexGrid).
    /// Order: E, W, NE, SW, SE, NW (consistent with AxialGrid flat-array offsets).
    /// Pairs are opposite directions — NOT circularly ordered.
    /// </summary>
    public static readonly int2[] NeighborsPointyTop =
    {
        new int2(1, 0),   // E
        new int2(-1, 0),  // W
        new int2(0, 1),   // NE
        new int2(0, -1),  // SW
        new int2(1, -1),  // SE
        new int2(-1, 1)   // NW
    };

    /// <summary>
    /// The 6 axial neighbor directions for flat-top orientation (fine VertexGrid),
    /// in circular CCW order. Consecutive entries are always mutually adjacent,
    /// enabling correct triangle formation when triangulating the flat-top lattice.
    /// Order: upper-right → top → upper-left → lower-left → bottom → lower-right.
    /// </summary>
    public static readonly int2[] NeighborsFlatTop =
    {
        new int2(1, 0),   // upper-right
        new int2(0, 1),   // top
        new int2(-1, 1),  // upper-left
        new int2(-1, 0),  // lower-left
        new int2(0, -1),  // bottom
        new int2(1, -1)   // lower-right
    };

    public const int NeighborCount = 6;

    // =========================================================================
    // Conversions
    // =========================================================================

    /// <summary>
    /// Axial integer coordinate → world position (Y = 0).
    /// </summary>
    public static float3 AxialToWorldPointTop(int2 coord, float cellSize)
    {
        var x = cellSize * math.sqrt(3f) * (coord.x + coord.y * 0.5f);
        var z = cellSize * 1.5f * coord.y;
        return new float3(x, 0f, z);
    }

    public static float3 AxialToWorld(int2 coord, float cellSize, AxialOrientation orientation)
    {
        return orientation == AxialOrientation.PointyTop
            ? AxialToWorldPointTop(coord, cellSize)
            : AxialToWorldFlatTop(coord, cellSize);
    }

    /// <summary>
    /// Axial integer coordinate → world XZ as float2 (for 2D mesh generation).
    /// </summary>
    public static float2 AxialToWorld2D(int2 coord, float cellSize)
    {
        var x = cellSize * math.sqrt(3f) * (coord.x + coord.y * 0.5f);
        var z = cellSize * 1.5f * coord.y;
        return new float2(x, z);
    }

    /// <summary>
    /// World position → axial integer coordinate (uses XZ plane, ignores Y).
    /// </summary>
    public static int2 WorldToAxial(float3 worldPos, float cellSize)
    {
        var r = (int)math.round(worldPos.z / (cellSize * 1.5f));
        var q = (int)math.round(worldPos.x / (cellSize * math.sqrt(3f)) - r * 0.5f);
        return new int2(q, r);
    }

    /// <summary>
    /// World XZ position (as float2) → axial integer coordinate.
    /// </summary>
    public static int2 WorldToAxial(float2 worldXZ, float cellSize)
    {
        var r = (int)math.round(worldXZ.y / (cellSize * 1.5f));
        var q = (int)math.round(worldXZ.x / (cellSize * math.sqrt(3f)) - r * 0.5f);
        return new int2(q, r);
    }

    // =========================================================================
    // Flat-top conversions (used by VertexGrid)
    // =========================================================================

    /// <summary>
    /// Flat-top axial integer coordinate → world position (Y = 0).
    /// x = c * 1.5 * q
    /// z = c * √3 * (r + q/2)
    /// </summary>
    public static float3 AxialToWorldFlatTop(int2 coord, float cellSize)
    {
        var x = cellSize * 1.5f * coord.x;
        var z = cellSize * math.sqrt(3f) * (coord.y + coord.x * 0.5f);
        return new float3(x, 0f, z);
    }

    /// <summary>
    /// World position → flat-top axial integer coordinate (uses XZ plane, ignores Y).
    /// q = round(x / (c * 1.5))
    /// r = round(z / (c * √3) − q / 2)
    /// </summary>
    public static int2 WorldToAxialFlatTop(float3 worldPos, float cellSize)
    {
        var q = (int)math.round(worldPos.x / (cellSize * 1.5f));
        var r = (int)math.round(worldPos.z / (cellSize * math.sqrt(3f)) - q * 0.5f);
        return new int2(q, r);
    }

    // =========================================================================
    // Cell size helpers
    // =========================================================================

    /// <summary>
    /// Cell size for fine vertex grid.
    /// VertexGrid uses FlatTop axial orientation where adjacent cell distance = cellSize * √3.
    /// Mesh vertices are spaced hexSize/subdivisions apart, so cellSize = hexSize / (subdivisions * √3).
    /// </summary>
    public static float VertexCellSize(float hexSize, int subdivisions) => hexSize / (subdivisions * math.sqrt(3f));

    /// <summary>
    /// Cell size for coarse hex grid = hexSize.
    /// </summary>
    public static float HexCellSize(float hexSize) => hexSize;

    // =========================================================================
    // Neighbor utilities
    // =========================================================================

    /// <summary>
    /// Get the i-th neighbor direction (0..5).
    /// </summary>
    public static int2 GetNeighborDir(int index) => NeighborsPointyTop[index];

    /// <summary>
    /// Get the axial coordinate of the i-th neighbor of the given position.
    /// </summary>
    public static int2 GetNeighbor(int2 position, int index) => position + NeighborsPointyTop[index];

    /// <summary>
    /// Check if two axial coordinates are neighbors (distance = 1 in hex grid).
    /// </summary>
    public static bool AreNeighbors(int2 a, int2 b)
    {
        var d = a - b;
        for (var i = 0; i < 6; i++)
            if (math.all(d == NeighborsPointyTop[i]))
                return true;
        return false;
    }

    /// <summary>
    /// Hex distance (number of steps) between two axial coordinates.
    /// </summary>
    public static int Distance(int2 a, int2 b)
    {
        var d = a - b;
        return (math.abs(d.x) + math.abs(d.y) + math.abs(d.x + d.y)) / 2;
    }
}
