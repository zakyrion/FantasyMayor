using System;
using Modules.AxialSystem;
using Unity.Mathematics;

namespace Domains.Map.Hex.Data
{
    /// <summary>
    ///     Single vertex in the hex vector field.
    ///     OwnerCount = 0 means "ghost vertex" — exists in axial coordinate space
    ///     but has no mesh counterpart (MeshIndex = -1).
    /// </summary>
    public struct HexVertex
    {
        public float3 Position; // world XYZ, Y = height
        public int MeshIndex; // index into mesh.vertices; -1 = no mesh

        private HexCoord _owner0; // axial coords of owning hex (coarse hex grid)
        private HexCoord _owner1; // unused if OwnerCount < 2
        private HexCoord _owner2; // unused if OwnerCount < 3
        private int _ownerCount; // 0 = ghost, 1 = interior, 2 = edge, 3 = corner

        public int OwnerCount => _ownerCount;

        /// <summary>Returns the owner at the given index (0–OwnerCount-1).</summary>
        public HexCoord this[int index] => index switch
        {
            0 => _owner0,
            1 => _owner1,
            2 => _owner2,
            _ => throw new IndexOutOfRangeException($"HexVertex owner index {index} out of range (OwnerCount={_ownerCount})")
        };

        /// <summary>Appends hexCoord as the next owner. Throws if already 3 owners.</summary>
        public void AddOwner(HexCoord hexCoord)
        {
            switch (_ownerCount)
            {
                case 0: _owner0 = hexCoord; break;
                case 1: _owner1 = hexCoord; break;
                case 2: _owner2 = hexCoord; break;
                default: throw new InvalidOperationException("HexVertex already has 3 owners");
            }
            _ownerCount++;
        }

        /// <summary>Removes hexCoord from owners, shifting remaining entries down. No-op if not found.</summary>
        public void RemoveOwner(HexCoord hexCoord)
        {
            for (var i = 0; i < _ownerCount; i++)
            {
                if (!this[i].Equals(hexCoord)) continue;
                if (i < 1 && _ownerCount > 1) _owner0 = _owner1;
                if (i < 2 && _ownerCount > 2) _owner1 = _owner2;
                _ownerCount--;
                return;
            }
        }

        /// <summary>Returns true if hexCoord is one of this vertex's owners.</summary>
        public bool OwnedBy(HexCoord hexCoord)
        {
            return _ownerCount > 0 && (
                _owner0.Equals(hexCoord) ||
                _ownerCount > 1 && _owner1.Equals(hexCoord) ||
                _ownerCount > 2 && _owner2.Equals(hexCoord));
        }
    }
}
