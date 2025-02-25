using HW.Data;
using HW.Data.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class WorldTerrainTypeDecorationSystem : SystemBase
{
    private EntityQuery _centerPointsQuery;
    private EntityQuery _commandQuery;
    private EntityQuery _cornerPointsQuery;

    private NativeList<Entity> _decoratedPoints;
    private Random _random;
    protected NativeArray<int2> Neighbours = new(6, Allocator.Persistent);

    protected override void OnCreate()
    {
        MapSpawnSettings.InitSubHexCollections(Neighbours);
        _commandQuery = GetEntityQuery(ComponentType.ReadWrite<WorldGenerationCommand>());
        _centerPointsQuery = GetEntityQuery(ComponentType.ReadWrite<CenterPointElement>(),
            ComponentType.ReadWrite<PointComponent>(), ComponentType.ReadWrite<HexPositionComponent>());
        _cornerPointsQuery = GetEntityQuery(ComponentType.ReadOnly<CornerParentsElement>(),
            ComponentType.ReadWrite<PointComponent>());
    }

    protected override void OnDestroy()
    {
        Neighbours.Dispose();
    }

    protected override void OnUpdate()
    {
        if (_commandQuery.CalculateEntityCount() > 0)
        {
            var commandsEntities = _commandQuery.ToEntityArray(Allocator.Temp);
            var command = SystemAPI.GetComponentRW<WorldGenerationCommand>(commandsEntities[0]);

            switch (command.ValueRO.Stage)
            {
                case WorldGenerationStage.PlaceTerrainTypes:
                {
                    var dict = InitCenterPointsDictionary();
                    var hexCount = MapSpawnSettings.HexagonCount(command.ValueRO.WaveCount);
                    _decoratedPoints = new NativeList<Entity>(Allocator.Temp);
                    _random = new Random(command.ValueRO.Seed);

                    var commandRO = command.ValueRO;

                    PutWater(dict, (int) (hexCount * commandRO.WaterPercentage), commandRO.WaveCount);
                    PutMountains(dict, commandRO.MountsCount, commandRO.HillsPercentage);
                    SetWeightForCorners();

                    command.ValueRW.Stage = WorldGenerationStage.CreateHexes;
                }
                    break;
            }
        }
    }

    private NativeHashMap<int2, Entity> InitCenterPointsDictionary()
    {
        var points = _centerPointsQuery.ToEntityArray(Allocator.Temp);
        var dict = new NativeHashMap<int2, Entity>(points.Length, Allocator.Temp);

        foreach (var p in points)
        {
            dict.Add(SystemAPI.GetComponent<HexPositionComponent>(p).Position, p);
        }

        return dict;
    }

    private void SetWeightForCorners()
    {
        var dict = InitCenterPointsDictionary();
        var corners = _cornerPointsQuery.ToEntityArray(Allocator.Temp);

        foreach (var corner in corners)
        {
            var pointComponent = SystemAPI.GetComponentRW<PointComponent>(corner);
            var parents = SystemAPI.GetBuffer<CornerParentsElement>(corner);

            var weight = 0f;
            var count = 0;

            foreach (var parent in parents)
            {
                if (dict.TryGetValue(parent.ParentPosition, out var entity))
                {
                    weight += SystemAPI.GetComponent<PointComponent>(entity).Weight;
                    count++;
                }
            }

            pointComponent.ValueRW.Weight = weight / count;
            ref var translation = ref SystemAPI.GetComponentRW<LocalTransform>(corner).ValueRW;
            translation.Position =
                new float3(translation.Position.x, pointComponent.ValueRO.Weight, translation.Position.z);
        }
    }

    private void PutMountains(NativeHashMap<int2, Entity> dict, int count, float chance)
    {
        var points = _centerPointsQuery.ToEntityArray(Allocator.Temp);
        var allowToSpawn = new NativeList<Entity>(Allocator.Temp);

        foreach (var p in points)
        {
            if (!_decoratedPoints.Contains(p))
            {
                allowToSpawn.Add(p);
            }
        }

        while (count > 0)
        {
            var index = _random.NextInt(allowToSpawn.Length);
            var entity = allowToSpawn[index];
            allowToSpawn.RemoveAt(index);

            if (_decoratedPoints.Contains(entity))
            {
                continue;
            }

            _decoratedPoints.Add(entity);

            count--;

            ref var translation = ref SystemAPI.GetComponentRW<LocalTransform>(entity).ValueRW;
            translation.Position = new float3(translation.Position.x, 1f, translation.Position.z);

            var pointComponent = SystemAPI.GetComponentRW<PointComponent>(entity);
            pointComponent.ValueRW.Weight = 1f;

            var position = SystemAPI.GetComponent<HexPositionComponent>(entity).Position;
            for (var i = 0; i < 6; i++)
            {
                var neighborPosition = position + Neighbours[i];
                if (dict.ContainsKey(neighborPosition) && _random.NextFloat() <= chance)
                {
                    var hillEntity = dict[neighborPosition];
                    _decoratedPoints.Add(hillEntity);

                    ref var hillTranslation = ref SystemAPI.GetComponentRW<LocalTransform>(hillEntity).ValueRW;
                    hillTranslation.Position = new float3(hillTranslation.Position.x, .5f, hillTranslation.Position.z);

                    var hillPointComponent = SystemAPI.GetComponentRW<PointComponent>(hillEntity);
                    hillPointComponent.ValueRW.Weight = 0.5f;
                }
            }
        }
    }

    private void PutWater(NativeHashMap<int2, Entity> dict, int count, int wave)
    {
        var startCount = count;
        var point = MapSpawnSettings.GetRandomHexPosition(wave - 1);

        var spawnPositions = new NativeList<int2>(Allocator.Temp);
        var existPositions = new NativeList<int2>(Allocator.Temp);

        spawnPositions.Add(point);

        while (count-- > 0)
        {
            var nextIterationSpawnPositions = new NativeList<int2>(Allocator.Temp);

            foreach (var position in spawnPositions)
            {
                if (existPositions.Contains(position))
                {
                    continue;
                }

                if (count == 0)
                {
                    break;
                }

                count--;
                existPositions.Add(position);

                for (var i = 0; i < 6; i++)
                {
                    var chance = 1 - (float) count / startCount < 0.7f ? 1 : 0.25f;

                    var neighborPosition = position + Neighbours[i];
                    if (dict.ContainsKey(neighborPosition) && !existPositions.Contains(neighborPosition) &&
                        _random.NextFloat() <= chance)
                    {
                        nextIterationSpawnPositions.Add(neighborPosition);
                    }
                }
            }

            spawnPositions = nextIterationSpawnPositions;
        }

        foreach (var position in existPositions)
        {
            var entity = dict[position];
            dict.Remove(position);
            _decoratedPoints.Add(entity);
            ref var translation = ref SystemAPI.GetComponentRW<LocalTransform>(entity).ValueRW;
            translation.Position = new float3(translation.Position.x, -.2f, translation.Position.z);
            var pointComponent = SystemAPI.GetComponentRW<PointComponent>(entity);
            pointComponent.ValueRW.Weight = -0.2f;
        }
    }
}