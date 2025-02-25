using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct ApplyRectTextureToVectorFieldSimpleJob : IJob
{
    public float Height;
    public int TextureResolution;
    public Rect Rect;

    //TODO replace by TerrainHeightmap
    [ReadOnly] public NativeHashMap<int2, float> HeightMap;
    public NativeHashMap<int2, FieldsVector> HexVectors;

    public void Execute()
    {
        var rectCenterGridPosition = HexVectorUtil.CalculateGridPosition(Rect);

        var checkQueue = new NativeQueue<int2>(Allocator.TempJob);
        var checkedPositions = new NativeHashSet<int2>(HexVectors.Count, Allocator.TempJob);

        checkQueue.Enqueue(rectCenterGridPosition);

        while (checkQueue.Count > 0)
        {
            var toCheck = checkQueue.Dequeue();

            if (!checkedPositions.Contains(toCheck) && HexVectors.TryGetValue(toCheck, out var checkVector))
            {
                checkedPositions.Add(toCheck);
                var texturePosition = new float2();

                if (WorldToTexturePosition(checkVector.WorldPosition, ref texturePosition))
                {
                    var coords = new int2((int) math.floor(texturePosition.x), (int) math.floor(texturePosition.y));

                    if (HeightMap.TryGetValue(coords, out var textureHeight))
                    {
                        var xNext = math.min(TextureResolution - 1, coords.x + 1);
                        var yNext = math.min(TextureResolution - 1, coords.y + 1);

                        var koefX = texturePosition.x - coords.x;
                        var koefY = texturePosition.y - coords.y;

                        var height1 = textureHeight;
                        var height2 = HeightMap[new int2(xNext, coords.y)];
                        var height3 = HeightMap[new int2(coords.x, yNext)];
                        var height4 = HeightMap[new int2(xNext, yNext)];

                        var height = math.lerp(math.lerp(height1, height2, koefX), math.lerp(height3, height4, koefX),
                            koefY);

                        var hexVector = HexVectors[toCheck];
                        hexVector.SetHeight(height * Height);
                        HexVectors[toCheck] = hexVector;

                        for (var i = 0; i < 6; i++)
                        {
                            var neighbour = HexVectorUtil.Neighbour(i) + toCheck;

                            if (!checkedPositions.Contains(neighbour)) checkQueue.Enqueue(neighbour);
                        }
                    }
                }
            }
        }


        checkedPositions.Dispose();
        checkQueue.Dispose();
    }

    private bool WorldToTexturePosition(float3 worldPosition, ref float2 texturePosition)
    {
        if (worldPosition.x < Rect.xMin || worldPosition.x > Rect.xMax || worldPosition.z < Rect.yMin ||
            worldPosition.z > Rect.yMax)
            return false;

        texturePosition = math.remap(Rect.min, Rect.max,
            new float2(), new float2(TextureResolution, TextureResolution), worldPosition.xz);

        return true;
    }
}