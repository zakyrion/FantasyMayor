using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct ApplyRectTextureToVectorFieldJob : IJob
{
    public float Epsilon;
    public int TextureResolution;
    public Rect Rect;

    [ReadOnly] public NativeHashMap<int2, float> HeightMap;
    public NativeHashMap<int2, FieldsVector> HexVectors;

    public void Execute()
    {
        var rectCenterGridPosition = HexVectorUtil.CalculateGridPosition(Rect);

        var checkQueue = new NativeQueue<int2>(Allocator.TempJob);
        var checkedPositions = new NativeHashSet<int2>(HexVectors.Count, Allocator.TempJob);

        var applyHeightmap = new NativeHashMap<int2, VectorTuple>(HexVectors.Count, Allocator.TempJob);

        checkQueue.Enqueue(rectCenterGridPosition);

        while (checkQueue.Count > 0)
        {
            var toCheck = checkQueue.Dequeue();

            if (!checkedPositions.Contains(toCheck) && HexVectors.TryGetValue(toCheck, out var checkVector))
            {
                checkedPositions.Add(toCheck);
                var height = checkVector.WorldPosition.y;
                var maxHeight = 0f;

                var texturePosition = new int2();
                var added = false;
                if (WorldToTexturePosition(checkVector.WorldPosition, ref texturePosition))
                {
                    if (HeightMap.TryGetValue(texturePosition, out var textureHeight))
                    {
                        height = textureHeight;

                        added = true;

                        for (var i = 0; i < 6; i++)
                        {
                            var neighbour = HexVectorUtil.Neighbour(i) + toCheck;

                            if (!checkedPositions.Contains(neighbour)) checkQueue.Enqueue(neighbour);
                        }
                    }
                }

                for (var i = 0; i < 6; i++)
                {
                    var neighbour = HexVectorUtil.Neighbour(i) + toCheck;

                    if (applyHeightmap.TryGetValue(neighbour, out var tuple))
                        maxHeight = math.max(maxHeight, tuple.Height);
                    else if (HexVectors.TryGetValue(neighbour, out var neighbourVector))
                        maxHeight = math.max(maxHeight, neighbourVector.WorldPosition.y);
                }

                height = math.max(height, maxHeight - Epsilon);

                applyHeightmap.TryAdd(toCheck,
                    new VectorTuple(toCheck, height));

                if (!added && math.abs(height - checkVector.WorldPosition.y) > Epsilon)
                {
                    for (var i = 0; i < 6; i++)
                    {
                        var neighbour = HexVectorUtil.Neighbour(i) + toCheck;

                        if (!checkedPositions.Contains(neighbour)) checkQueue.Enqueue(neighbour);
                    }
                }
            }
        }


        checkedPositions.Dispose();
        checkQueue.Dispose();

        foreach (var tuple in applyHeightmap)
        {
            if (HexVectors.TryGetValue(tuple.Key, out var hexVector))
            {
                hexVector.SetHeight(tuple.Value.Height);
                HexVectors[tuple.Key] = hexVector;
            }
        }

        applyHeightmap.Dispose();
    }

    private bool WorldToTexturePosition(float3 worldPosition, ref int2 texturePosition)
    {
        if (worldPosition.x < Rect.xMin || worldPosition.x > Rect.xMax || worldPosition.z < Rect.yMin ||
            worldPosition.z > Rect.yMax)
            return false;

        var coords = math.remap(Rect.min, Rect.max,
            new float2(), new float2(TextureResolution, TextureResolution), worldPosition.xz);

        texturePosition = new int2((int) coords.x, (int) coords.y);
        return true;
    }
}