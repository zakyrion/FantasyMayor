using System;

namespace Modules.Hexes.DataTypes
{
    public struct HexData : IEquatable<HexData>
    {
        public HexId HexId;

        public bool Equals(HexData other)
        {
            return Equals(HexId, other.HexId);
        }

        public override bool Equals(object obj)
        {
            return obj is HexData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HexId != null ? HexId.GetHashCode() : 0;
        }
    }
}
