using System.Collections.Generic;
using JetBrains.Annotations;
using Modules.Hexes.DataTypes;
using Unity.Collections;
using Unity.Mathematics;

namespace Modules.Hexes.DataLayer
{
    [UsedImplicitly]
    public struct HexesViewDataLayer
    {
        public List<HexViewData> Hexes;
        private readonly Dictionary<HexId, HexViewData> _hexCache;
        private NativeHashMap<int2, FieldsVector> _vectorField;
        //public ref NativeHashMap<int2, FieldsVector> HexVectors => ref _vectorField;


        public HexViewData this[HexId index] => _hexCache[index];
        public HexViewData this[int x, int y] => _hexCache[new HexId { Coords = new int2(x, y) }];
        public HexViewData this[int index] => Hexes[index];


        public void AddHex(HexViewData hexData)
        {
            Hexes.Add(hexData);
            _hexCache.Add(hexData.HexId, hexData);
        }

        public void Dispose()
        {
            _vectorField.Dispose();
        }

        public bool Exists(HexId hexCoords)
        {
            return _hexCache.ContainsKey(hexCoords);
        }

        public HexViewData GetHex(HexId hexCoords)
        {
            return _hexCache[hexCoords];
        }
    }
}
