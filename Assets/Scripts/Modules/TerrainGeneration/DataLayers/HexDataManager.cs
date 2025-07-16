using Unity.Mathematics;

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