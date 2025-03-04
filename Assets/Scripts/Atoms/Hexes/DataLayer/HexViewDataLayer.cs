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

    /*
     * public List<int2>[] GetNeighboringHexesForCorners(int2 hexCoords)
       {
           var q0 = hexCoords.x;
           var r0 = hexCoords.y;
           return new List<int2>[6]
           {
               new List<int2>{new int2(q0, r0+1), new int2(q0+1, r0)}, // Neighbors for Corner 1
               new List<int2>{new int2(q0+1, r0), new int2(q0+1, r0-1)}, // Neighbors for Corner 2
               new List<int2>{new int2(q0+1, r0-1), new int2(q0, r0-1)}, // Neighbors for Corner 3
               new List<int2>{new int2(q0, r0-1), new int2(q0-1, r0)}, // Neighbors for Corner 4
               new List<int2>{new int2(q0-1, r0), new int2(q0-1, r0+1)}, // Neighbors for Corner 5
               new List<int2>{new int2(q0-1, r0+1), new int2(q0, r0+1)}  // Neighbors for Corner 6
           };
       }

       Можна визначити точки кордону для кожного гекса як ті, які мають хоча б один гекс як сусіда котрий не належить до списку
       У кожної точки є свій список гексік до яких вона належить, їх 3, від цього можна відштовхуватися в пошуку
     */
}