using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(3)]
public struct CornerParentsElement : IBufferElementData
{
    public int2 ParentPosition;
}