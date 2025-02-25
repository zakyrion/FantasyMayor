using Unity.Entities;

public struct PointComponent : IComponentData
{
    public PointType Type;
    public float Weight;
}