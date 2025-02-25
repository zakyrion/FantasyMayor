using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct MultiplyMapsJob : IJob
{
    public NativeHashMap<int2, float> MapA;
    public NativeHashMap<int2, float> MapB;

    public int Size;

    public void Execute()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                var index = new int2(x, y);
                var valueA = MapA[index];
                var valueB = MapB[index];

                MapA[index] = valueA * valueB;
            }
        }
    }
}

//Add job
public struct AddMapsJob : IJob
{
    public NativeHashMap<int2, float> MapA;
    public NativeHashMap<int2, float> MapB;

    public int Size;

    public void Execute()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                var index = new int2(x, y);
                var valueA = MapA[index];
                var valueB = MapB[index];

                MapA[index] = valueA + valueB;
            }
        }
    }
}

//Subtract job
public struct SubtractMapsJob : IJob
{
    public NativeHashMap<int2, float> MapA;
    public NativeHashMap<int2, float> MapB;

    public int Size;

    public void Execute()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                var index = new int2(x, y);
                var valueA = MapA[index];
                var valueB = MapB[index];

                MapA[index] = valueA - valueB;
            }
        }
    }
}

//Divide job
public struct DivideMapsJob : IJob
{
    public NativeHashMap<int2, float> MapA;
    public NativeHashMap<int2, float> MapB;

    public int Size;

    public void Execute()
    {
        for (var x = 0; x < Size; x++)
        {
            for (var y = 0; y < Size; y++)
            {
                var index = new int2(x, y);
                var valueA = MapA[index];
                var valueB = MapB[index];

                MapA[index] = valueA / valueB;
            }
        }
    }
}