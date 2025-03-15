using HW.Data.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class HexGenerationSystem : SystemBase
{
    private EntityQuery _centerPointsQuery;
    private EntityQuery _commandQuery;
    private EntityQuery _cornerPointsQuery;

    private HexMonoFactory _hexMonoFactory;

    public void Init(HexMonoFactory hexMonoFactory)
    {
        _hexMonoFactory = hexMonoFactory;
    }

    protected override void OnCreate()
    {
        _commandQuery = GetEntityQuery(ComponentType.ReadWrite<WorldGenerationCommand>());

        _centerPointsQuery = GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadWrite<CenterPointElement>(), ComponentType.ReadWrite<PointComponent>(),
            ComponentType.ReadWrite<HexPositionComponent>());

        _cornerPointsQuery = GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(),
            ComponentType.ReadOnly<CornerParentsElement>(),
            ComponentType.ReadWrite<PointComponent>());
    }

    protected override void OnUpdate()
    {
        if (_commandQuery.CalculateEntityCount() > 0)
        {
            var commandsEntities = _commandQuery.ToEntityArray(Allocator.Temp);
            var command = SystemAPI.GetComponentRW<WorldGenerationCommand>(commandsEntities[0]);

            switch (command.ValueRO.Stage)
            {
                case WorldGenerationStage.CreateHexes:
                {
                    command.ValueRW.Stage = WorldGenerationStage.Await;
                    var centerPoints = _centerPointsQuery.ToEntityArray(Allocator.TempJob);

                    foreach (var centerEntity in centerPoints)
                    {
                        var points = new NativeList<float3>(7, Allocator.Persistent);

                        var centerTransform = SystemAPI.GetComponentRO<LocalTransform>(centerEntity);
                        points.Add(centerTransform.ValueRO.Position);

                        var centerPointElement = SystemAPI.GetBuffer<CenterPointElement>(centerEntity);

                        foreach (var pointEntity in centerPointElement)
                        {
                            var pointTransform = SystemAPI.GetComponentRO<LocalTransform>(pointEntity.Point);
                            points.Add(pointTransform.ValueRO.Position);
                        }

                        _hexMonoFactory.SpawnHex(points);
                    }
                }
                    break;

                case WorldGenerationStage.DetailedHexes:
                    command.ValueRW.Stage = WorldGenerationStage.Finish;
                    break;
            }
        }
    }
}