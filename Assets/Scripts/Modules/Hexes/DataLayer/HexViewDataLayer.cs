using System.Collections.Generic;
using Atoms.Hexes.DataTypes;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;

namespace Atoms.Hexes.DataLayer
{
    [UsedImplicitly]
    public class HexViewDataLayer
    {
        public List<HexViewData> Hexes = new();
        private readonly Dictionary<HexId, HexViewData> _hexCache = new();
        private NativeHashMap<int2, FieldsVector> _vectorField;
        public ref NativeHashMap<int2, FieldsVector> HexVectors => ref _vectorField;

        public HexViewData this[HexId index] => _hexCache[index];
        public HexViewData this[int x, int y] => _hexCache[new HexId { Coords = new int2(x, y) }];
        public HexViewData this[int index] => Hexes[index];

        public HexViewDataLayer()
        {
            _vectorField = new NativeHashMap<int2, FieldsVector>(100, Allocator.Persistent);
        }

        public void AddHex(HexViewData hexData)
        {
            Hexes.Add(hexData);
            _hexCache.Add(hexData.HexId, hexData);
        }

        public bool Exists(HexId hexCoords)
        {
            return _hexCache.ContainsKey(hexCoords);
        }

        public HexViewData GetHex(HexId hexCoords)
        {
            return _hexCache[hexCoords];
        }

        public void Dispose()
        {
            _vectorField.Dispose();
        }
    }
}
