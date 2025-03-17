using System.Collections.Generic;
using Unity.Burst;
using Unity.Mathematics;

/// <summary>
/// Utility class for working with hexagons.
/// </summary>
[BurstCompile]
public static class HexUtil
{
    public static int Count => 6;

    /// <summary>
    /// Returns all neighborings.
    /// </summary>
    /// <returns>An enumerable that yields the neighboring hexagons.</returns>
    public static IEnumerable<int2> Neighbours()
    {
        for (var i = 0; i <= 5; i++) yield return Neighbour(i);
    }

    /// <summary>
    /// Returns all neighboring hexagons around the given position.
    /// </summary>
    /// <param name="position">The position of the hexagon.</param>
    /// <returns>An enumerable that yields the neighboring hexagons.</returns>
    public static IEnumerable<int2> Neighbours(int2 position)
    {
        for (var i = 0; i <= 5; i++) yield return Neighbour(i) + position;
    }
    
    public static IEnumerable<HexId> Neighbours(HexId hexId)
    {
        for (var i = 0; i <= 5; i++) yield return new HexId() {Coords = Neighbour(i) + hexId.Coords};
    }

    public static IEnumerable<(float3, float3)> Edges(float3 center, float size)
    {
        for (var i = 0; i <= 5; i++) yield return GetEdge(i, center, size);
    }

    /// <summary>
    /// Returns the neighboring hexagon at the specified index.
    /// </summary>
    /// <param name="index">The index of the neighboring hexagon, between 0 and 5 inclusive.</param>
    /// <returns>The neighboring hexagon at the specified index.</returns>
    public static int2 Neighbour(int index)
    {
        return index switch
        {
            0 => new int2(0, -1), // right
            5 => new int2(1, -1), // right bottom
            4 => new int2(1, 0), // left bottom 
            3 => new int2(0, 1), // left
            2 => new int2(-1, 1), // left top
            1 => new int2(-1, 0), // right top
            _ => default
        };
    }
    
    public static bool IsNeighbour(int2 a, int2 b)
    {
        return math.any(math.abs(a - b) == new int2(1, 1)) || math.any(math.abs(a - b) == new int2(1, 0));
    }

    /// <summary>
    /// Returns the position of the neighboring hexagon at the specified index.
    /// </summary>
    /// <param name="index">The index of the neighboring hexagon. Valid values are from 0 to 5.</param>
    /// <param name="hexPosition">The position of the current hexagon.</param>
    /// <returns>The position of the neighboring hexagon.</returns>
    public static int2 Neighbour(int index, int2 hexPosition)
    {
        hexPosition += Neighbour(index);
        return hexPosition;
    }

    /// <summary>
    /// Returns the vertex at the specified edge for a hexagon with the given center and size.
    /// </summary>
    /// <param name="edgeIndex">The index of edge, between 0 and 5 inclusive.</param>
    /// <param name="center">The center of the hexagon.</param>
    /// <param name="size">The size of the hexagon.</param>
    /// <returns>The vertex at the specified index.</returns>
    public static float3 GetVertex(int edgeIndex, float3 center, float size)
    {
        var angleDeg = 60 * ((edgeIndex + 4) % 6);
        var angleRad = math.radians(angleDeg);
        return new float3(center.x + size * math.cos(angleRad), center.y, center.z + size * math.sin(angleRad));
    }

    /// <summary>
    /// Returns the index of the neighboring hexagon that matches the given position.
    /// </summary>
    /// <param name="neighbour">The position of the neighboring hexagon.</param>
    /// <returns>The index of the neighboring hexagon that matches the given position. Returns -1 if no match is found.</returns>
    public static int NeighbourToIndex(int2 neighbour)
    {
        for (var i = 0; i < 6; i++)
        {
            if (Neighbour(i).Equals(neighbour))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Returns the edge vertices at the specified neighboring hexagon for a hexagon with the given center and size.
    /// </summary>
    /// <param name="neighbour">The position of the neighboring hexagon.</param>
    /// <param name="center">The center of the hexagon.</param>
    /// <param name="size">The size of the hexagon.</param>
    /// <returns>The edge vertices at the specified neighboring hexagon.</returns>
    public static (float3, float3) GetEdge(int2 neighbour, float3 center, float size)
    {
        var firstVertex = GetVertex(NeighbourToIndex(neighbour), center, size);
        var secondVertex = GetVertex(NeighbourToIndex(neighbour) + 1, center, size);
        return (firstVertex, secondVertex);
    }

    /// <summary>
    /// Returns the edge at the specified index for a hexagon with the given center and size.
    /// </summary>
    /// <param name="index">The index of the edge, between 0 and 5 inclusive.</param>
    /// <param name="center">The center of the hexagon.</param>
    /// <param name="size">The size of the hexagon.</param>
    /// <returns>The edge represented by a tuple of two vertices.</returns>
    public static (float3, float3) GetEdge(int index, float3 center, float size)
    {
        var firstVertex = GetVertex(index, center, size);
        var secondVertex = GetVertex(index + 1, center, size);
        return (firstVertex, secondVertex);
    }

    /// <summary>
    /// Returns the center point of the edge at the specified index for a hexagon with the given center and size.
    /// </summary>
    /// <param name="index">The index of the edge, between 0 and 5 inclusive.</param>
    /// <param name="center">The center of the hexagon.</param>
    /// <param name="size">The size of the hexagon.</param>
    /// <returns>The center point of the edge.</returns>
    public static float3 GetCenterOfEdge(int index, float3 center, float size)
    {
        var (firstVertex, secondVertex) = GetEdge(index, center, size);
        return (firstVertex + secondVertex) / 2;
    }

    /// <summary>
    /// Returns the center point of the specified edge.
    /// </summary>
    /// <param name="index">The index of the edge (0 to 5)</param>
    /// <param name="center">The center of the hexagon</param>
    /// <param name="size">The size of the hexagon</param>
    /// <returns>The center point of the edge</returns>
    public static float3 GetCenterOfEdge(int2 neighbour, float3 center, float size)
    {
        var (firstVertex, secondVertex) = GetEdge(neighbour, center, size);
        return (firstVertex + secondVertex) / 2;
    }
    
    public static int GetSharedLineIndex(int2 neighbour)
    {
         return neighbour switch
         {
             { x: 1, y: -1 } => 0,
             { x: 0, y: -1 } => 1,
             { x: -1, y: 0 } => 2,
             { x: -1, y: 1 } => 3,
             { x: 0, y: 1 } => 4,
             { x: 1, y: 0 } => 5,
             _ => -1
         };
    }
}