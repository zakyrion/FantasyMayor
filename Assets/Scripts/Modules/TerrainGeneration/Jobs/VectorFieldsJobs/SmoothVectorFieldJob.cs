using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct SmoothVectorFieldJob : IJob
{
    public NativeHashMap<int2, FieldsVector> HexVectors;

    //TODO: add wave smoothing, and variable to set depth of smoothing
    
    public void Execute()
    {
        var newHexVectors = new NativeHashMap<int2, FieldsVector>(HexVectors.Count, Allocator.Temp);
        var neighbours = new NativeList<FieldsVector>(6, Allocator.Temp);

        foreach (var hexVector in HexVectors)
        {
            var average = hexVector.Value.Height;

            for (var i = 0; i < 6; i++)
            {
                var neighbour = hexVector.Value.GridPosition + HexVectorUtil.Neighbour(i);
                if (HexVectors.TryGetValue(neighbour, out var neighbourVector))
                {
                    neighbours.Add(neighbourVector);
                    average += neighbourVector.Height;
                }
            }

            if (neighbours.Length > 0)
                newHexVectors.TryAdd(hexVector.Key, hexVector.Value.CloneWithNewHeight(average / neighbours.Length));
            else
                newHexVectors.TryAdd(hexVector.Key, hexVector.Value);

            neighbours.Clear();
        }

        HexVectors = newHexVectors;
    }
}