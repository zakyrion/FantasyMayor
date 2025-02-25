using Unity.Burst;
using Unity.Mathematics;

[BurstCompile]
public struct VectorTuple
{
    public int2 Position;
    public float Height;

    public VectorTuple(int2 position, float height)
    {
        Position = position;
        Height = height;
    }
}