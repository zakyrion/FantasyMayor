using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

public class HexViewDataLayer : IDataContainer
{
    private readonly Dictionary<int2, HexViewData> _hexCache = new();
    private NativeHashMap<int2, FieldsVector> _vectorField;

    public List<HexViewData> Hexes = new();

    public HexViewDataLayer()
    {
        _vectorField = new NativeHashMap<int2, FieldsVector>(100, Allocator.Persistent);
    }

    public HexViewData this[int2 index] => _hexCache[index];
    public HexViewData this[int x, int y] => _hexCache[new int2(x, y)];
    public HexViewData this[int index] => Hexes[index];
    public ref NativeHashMap<int2, FieldsVector> HexVectors => ref _vectorField;

    public void Dispose()
    {
        _vectorField.Dispose();
    }

    public void AddHex(HexViewData hexData)
    {
        Hexes.Add(hexData);
        _hexCache.Add(hexData.Position, hexData);
    }

    public HexViewData GetHex(int2 hexCoords) => _hexCache[hexCoords];

    public bool Exists(int2 hexCoords) => _hexCache.ContainsKey(hexCoords);
}