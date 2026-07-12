using Unity.Mathematics;

namespace Modules.AxialSystem
{
    // Addresses a COARSE TILE (pointy-top grid). Structurally identical to VertexCoord but semantically
    // distinct — never mix them. Tile centre comes from AxialMath (AxialToWorld/AxialToWorld2D), never from
    // a centroid over the fine VertexGrid.
    public readonly struct HexCoord : IAxialCoord<HexCoord>
    {
        public int2 Value { get; }

        public HexCoord(int2 value) => Value = value;
        public HexCoord(int q, int r) => Value = new int2(q, r);

        public static HexCoord operator +(HexCoord a, int2 offset) => new(a.Value + offset);
        public static HexCoord operator -(HexCoord a, int2 offset) => new(a.Value - offset);
        public static int2 operator -(HexCoord a, HexCoord b) => a.Value - b.Value;

        public bool Equals(HexCoord other) => math.all(Value == other.Value);
        public override bool Equals(object obj) => obj is HexCoord other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"Hex({Value.x}, {Value.y})";

        public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);
    }
}
