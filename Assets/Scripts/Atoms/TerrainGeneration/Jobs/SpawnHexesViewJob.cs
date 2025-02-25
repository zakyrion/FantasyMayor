using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

public partial struct SpawnHexesViewJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<PointComponent> PointLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
    public EntityCommandBuffer CommandBuffer;

    private void Execute(in LocalTransform centerTransform, in PointComponent point, in HexPositionComponent position,
        in DynamicBuffer<CenterPointElement> pointsBuffer)
    {
        /*
                buffer.Add(new HexViewVertexBuffer
                {
                    Vertex = centerTransform.Position,
                    Weight = point.Weight
                });

                foreach (var pointElement in pointsBuffer)
                {
                    var pointEntity = pointElement.Point;

                    if (PointLookup.TryGetComponent(pointEntity, out var pointComponent) &&
                        TransformLookup.TryGetComponent(pointEntity, out var localTransformComponent))
                    {
                        buffer.Add(new HexViewVertexBuffer
                        {
                            Weight = pointComponent.Weight,
                            Vertex = localTransformComponent.Position
                        });
                    }
                }
                */
    }
}