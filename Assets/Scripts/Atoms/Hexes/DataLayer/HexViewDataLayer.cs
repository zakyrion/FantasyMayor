using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

public class HexViewDataLayer : IDataContainer
{
    private readonly Dictionary<HexId, HexViewData> _hexCache = new();
    private NativeHashMap<int2, FieldsVector> _vectorField;

    public List<HexViewData> Hexes = new();

    public HexViewDataLayer()
    {
        _vectorField = new NativeHashMap<int2, FieldsVector>(100, Allocator.Persistent);
    }

    public HexViewData this[HexId index] => _hexCache[index];
    public HexViewData this[int x, int y] => _hexCache[new HexId {Coords = new int2(x, y)}];
    public HexViewData this[int index] => Hexes[index];
    public ref NativeHashMap<int2, FieldsVector> HexVectors => ref _vectorField;

    public void Dispose()
    {
        _vectorField.Dispose();
    }

    public void AddHex(HexViewData hexData)
    {
        Hexes.Add(hexData);
        _hexCache.Add(hexData.HexId, hexData);
    }

    public HexViewData GetHex(HexId hexCoords) => _hexCache[hexCoords];

    public bool Exists(HexId hexCoords) => _hexCache.ContainsKey(hexCoords);
}