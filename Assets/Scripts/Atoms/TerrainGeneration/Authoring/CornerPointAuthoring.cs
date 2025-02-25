using Unity.Entities;
using UnityEngine;

public class CornerPointAuthoring : MonoBehaviour
{
}

public class CornerPointBaker : Baker<CornerPointAuthoring>
{
    public override void Bake(CornerPointAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent<PointComponent>(entity);
        AddBuffer<CornerParentsElement>(entity);
    }
}