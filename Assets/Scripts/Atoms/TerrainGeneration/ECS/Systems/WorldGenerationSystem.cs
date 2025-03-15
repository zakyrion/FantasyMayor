using HW.Data.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HW.Data.Systems
{
    public partial class WorldGenerationSystem : SystemBase
    {
        private EntityQuery _commandQuery;
        private NativeList<Entity> _cornerPoints;
        private EntityQuery _cornerSpawnerQuery;
        private EntityQuery _vertexSpawnerQuery;

        protected NativeArray<int2> Neighbours = new(6, Allocator.Persistent);

        protected override void OnCreate()
        {
            Neighbours[0] = new int2(0, -1);
            Neighbours[1] = new int2(1, -1);
            Neighbours[2] = new int2(1, 0);
            Neighbours[3] = new int2(0, 1);
            Neighbours[4] = new int2(-1, 1);
            Neighbours[5] = new int2(-1, 0);

            _commandQuery = EntityManager.CreateEntityQuery(ComponentType.ReadWrite<WorldGenerationCommand>());
            var archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<WorldGenerationCommand>());

            /*var entity = EntityManager.CreateEntity(archetype);
            EntityManager.SetComponentData(entity, new WorldGenerationCommand
            {
                WaveCount = 2,
                Seed = 1234,
                WaterPercentage = .2f,
                MountsCount = 5,
                HillsPercentage = .2f,
                Stage = WorldGenerationStage.SpawnPoints
            });

            _vertexSpawnerQuery = GetEntityQuery(ComponentType.ReadOnly<SpawnCenterPointBuffer>());
            _cornerSpawnerQuery = GetEntityQuery(ComponentType.ReadOnly<SpawnCornerPointBuffer>());*/

            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            if (_commandQuery.CalculateEntityCount() > 0)
            {
                var commandsEntities = _commandQuery.ToEntityArray(Allocator.Temp);
                var command = SystemAPI.GetComponentRW<WorldGenerationCommand>(commandsEntities[0]);

                switch (command.ValueRO.Stage)
                {
                    case WorldGenerationStage.SpawnPoints:
                        _cornerPoints = new NativeList<Entity>(Allocator.Persistent);
                        GenerateWorld(command.ValueRO);
                        command.ValueRW.Stage = WorldGenerationStage.PlaceTerrainTypes;
                        break;
                    case WorldGenerationStage.Finish:
                        EntityManager.DestroyEntity(commandsEntities[0]);
                        _cornerPoints.Dispose();
                        break;
                }
            }
        }

        protected override void OnDestroy()
        {
            if (_cornerPoints.IsCreated)
            {
                _cornerPoints.Dispose();
            }

            Neighbours.Dispose();
        }

        protected virtual void GenerateWorld(WorldGenerationCommand command)
        {
            SpawnCenters(command);
        }

        #region SpawnTheVertexes

        protected virtual void SpawnCenters(WorldGenerationCommand command)
        {
            var spawnPositions = new NativeList<int2>(Allocator.Temp);
            var existPositions = new NativeList<int2>(Allocator.Temp);

            spawnPositions.Add(new int2(0, 0));

            for (var i = 0; i < command.WaveCount; i++)
            {
                var nextIterationSpawnPositions = new NativeList<int2>(Allocator.Temp);

                foreach (var position in spawnPositions)
                {
                    if (existPositions.Contains(position))
                    {
                        continue;
                    }

                    existPositions.Add(position);

                    SpawnPoints(position);

                    for (var j = 0; j < 6; j++)
                    {
                        var neighborPosition = position + Neighbours[j];
                        if (!existPositions.Contains(neighborPosition))
                        {
                            nextIterationSpawnPositions.Add(neighborPosition);
                        }
                    }
                }

                spawnPositions.Dispose();
                spawnPositions = nextIterationSpawnPositions;
            }

            spawnPositions.Dispose();
            existPositions.Dispose();
        }

        private void SpawnPoints(int2 position)
        {
            var centerSpawnerEntities = _vertexSpawnerQuery.ToEntityArray(Allocator.Temp);
            var buffer = SystemAPI.GetBuffer<SpawnCenterPointBuffer>(centerSpawnerEntities[0]);

            var point = EntityManager.Instantiate(buffer[0].Prefab);
            EntityManager.SetComponentData(point, new PointComponent
            {
                Type = PointType.Center
            });

            var hexPositionComponent = new HexPositionComponent(position);
            SystemAPI.SetComponent(point, hexPositionComponent);
            
            var localTransformComponent = SystemAPI.GetComponentRW<LocalTransform>(point);
            localTransformComponent.ValueRW.Position =
                hexPositionComponent.CalculateTopAngleWorldPosition();

            var centerPointsBuffer = SystemAPI.GetBuffer<CenterPointElement>(point);

            for (var i = 0; i < 6; i++)
            {
                var cornerPoint = SpawnCornerPoint(localTransformComponent.ValueRO.Position,
                    quaternion.Euler(0, math.radians(60 * i), 0), MapSpawnSettings.HEX_SIZE);
                var cornerPointsBuffer = SystemAPI.GetBuffer<CornerParentsElement>(cornerPoint);
                cornerPointsBuffer.Add(new CornerParentsElement
                {
                    ParentPosition = position
                });

                centerPointsBuffer.Add(new CenterPointElement
                {
                    Point = cornerPoint
                });
            }
        }

        private Entity SpawnCornerPoint(float3 center, quaternion rotator, float distance)
        {
            var position = center + math.mul(rotator, new float3(0, 0, distance));

            for (var i = 0; i < _cornerPoints.Length; i++)
            {
                var cornerPoint = _cornerPoints[i];
                var localTransformComponent = SystemAPI.GetComponentRW<LocalTransform>(cornerPoint);
                if (math.distance(localTransformComponent.ValueRO.Position, position) < 0.1f)
                {
                    return cornerPoint;
                }
            }

            var cornerSpawnerEntities = _cornerSpawnerQuery.ToEntityArray(Allocator.Temp);
            var buffer = SystemAPI.GetBuffer<SpawnCornerPointBuffer>(cornerSpawnerEntities[0]);

            var point = EntityManager.Instantiate(buffer[0].Prefab);
            {
                var localTransformComponent = SystemAPI.GetComponentRW<LocalTransform>(point);
                localTransformComponent.ValueRW.Position = center + math.mul(rotator, new float3(0, 0, distance));
            }

            _cornerPoints.Add(point);
            return point;
        }

        #endregion
    }
}

/* wave

protected virtual void SpawnTheVertexes(WorldGenerationCommand command)
        {
            var spawnPositions = new NativeList<int2>(Allocator.Temp);
            var existPositions = new NativeList<int2>(Allocator.Temp);
            
            spawnPositions.Add(new int2(0, 0));

            for (int i = 0; i < command.WaveCount; i++)
            {
                var nextIterationSpawnPositions = new NativeList<int2>(Allocator.Temp);
                
                foreach (var position in spawnPositions)
                {
                    if (existPositions.Contains(position))
                    {
                        continue;
                    }
                    
                    existPositions.Add(position);
                    
                    
                    
                    for (var j = 0; j < 6; j++)
                    {
                        var neighborPosition = position + Neighbours[j];
                        if (!existPositions.Contains(neighborPosition))
                        {
                            nextIterationSpawnPositions.Add(neighborPosition);
                        }
                    }
                }
                
                spawnPositions.Dispose();
                spawnPositions = nextIterationSpawnPositions;
            }
            
            spawnPositions.Dispose();
            existPositions.Dispose();
        }
        }
        */