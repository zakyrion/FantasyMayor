using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(7)]
public struct HexViewVertexBuffer : IBufferElementData
{
    public float3 Vertex { get; set; }
    public float Weight { get; set; }
}