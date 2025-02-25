using Unity.Entities;

[InternalBufferCapacity(6)]
public struct CenterPointElement : IBufferElementData
{
    public Entity Point;
}