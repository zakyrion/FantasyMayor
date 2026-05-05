using System;
using Unity.Mathematics;

namespace Modules.AxialSystem
{
    public readonly struct VertexCoord : IAxialCoord<VertexCoord>
    {
        public int2 Value { get; }

        public VertexCoord(int2 value) => Value = value;
        public VertexCoord(int q, int r) => Value = new int2(q, r);

        public static VertexCoord operator +(VertexCoord a, int2 offset) => new(a.Value + offset);
        public static VertexCoord operator -(VertexCoord a, int2 offset) => new(a.Value - offset);
        public static int2 operator -(VertexCoord a, VertexCoord b) => a.Value - b.Value;

        public bool Equals(VertexCoord other) => math.all(Value == other.Value);
        public override bool Equals(object obj) => obj is VertexCoord other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => $"Vtx({Value.x}, {Value.y})";

        public static bool operator ==(VertexCoord a, VertexCoord b) => a.Equals(b);
        public static bool operator !=(VertexCoord a, VertexCoord b) => !a.Equals(b);
    }
}
