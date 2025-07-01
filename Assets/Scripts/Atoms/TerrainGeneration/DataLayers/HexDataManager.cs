using DataLayer.Core;
using Unity.Mathematics;

public class HexDataManager : EntityDataManager<HexId>
{
}

/// <summary>
///     Coords in 2d hex grid
/// </summary>
public record HexId
{
    public int2 Coords;

    public int x => Coords.x;
    public int y => Coords.y;

    public static implicit operator HexId(int2 coords) => new() {Coords = coords};
}