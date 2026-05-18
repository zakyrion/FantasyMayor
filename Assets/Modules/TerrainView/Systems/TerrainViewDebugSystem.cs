using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.HexesCore.Components;
using Modules.TerrainGenerator.Components;
using Modules.TerrainView.Components;
using UnityEngine;

namespace Modules.TerrainView.Systems
{
    [UsedImplicitly]
    internal sealed class TerrainViewDebugSystem : UpdatedSystem
    {
        private const int ExecutionPriority = TerrainViewSystem.ExecutionPriority + 1;
        private const float RayHeight = 5f;
        private const float RayDuration = 5f;

        private readonly EntitySet _configSet;
        private readonly EntitySet _hexSet;

        public override int Priority => ExecutionPriority;

        public TerrainViewDebugSystem(World world)
            : base(world.GetEntities()
                .WhenAdded<TerrainGenerationGenerateEventComponent>()
                .AsSet())
        {
            _hexSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexLevelComponent>()
                .AsSet();

            _configSet = world.GetEntities()
                .With<TerrainViewConfigComponent>()
                .AsSet();
        }

        protected override void Update(GameState state, in Entity entity)
        {
            if (_configSet.Count == 0)
            {
                entity.Dispose();
                return;
            }

            var cellSize = _configSet.GetEntities()[0]
                .Get<TerrainViewConfigComponent>()
                .CellSize;

            foreach (ref readonly var hexEntity in _hexSet.GetEntities())
            {
                var level = hexEntity.Get<HexLevelComponent>().Level;

                Color rayColor;
                if (level < 0)
                    rayColor = Color.blue;
                else if (level == 1)
                    rayColor = Color.green;
                else if (level == 2)
                    rayColor = Color.red;
                else
                    continue;

                var hexCoord = hexEntity.Get<HexIdComponent>().Coords;
                var center = AxialMath.AxialToWorld(hexCoord.Value, cellSize, AxialOrientation.PointyTop);
                Debug.DrawRay(new Vector3(center.x, center.y, center.z), Vector3.up * RayHeight, rayColor, RayDuration);
            }

            entity.Dispose();
        }

        public override void Dispose()
        {
            _hexSet.Dispose();
            _configSet.Dispose();
            base.Dispose();
        }
    }
}
