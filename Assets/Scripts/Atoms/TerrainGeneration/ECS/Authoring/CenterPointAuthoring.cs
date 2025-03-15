using Unity.Entities;
using UnityEngine;

public class CenterPointAuthoring : MonoBehaviour
{
}

public class CenterPointBaker : Baker<CenterPointAuthoring>
{
    public override void Bake(CenterPointAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.None);
        AddComponent<PointComponent>(entity);
        AddComponent<HexPositionComponent>(entity);
        AddBuffer<CenterPointElement>(entity);
    }
}