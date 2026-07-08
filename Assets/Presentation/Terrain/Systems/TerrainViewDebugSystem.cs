using System.Threading;
using Cysharp.Threading.Tasks;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.Boot.Core;
using Domains.Map.Hex.Components;
using Presentation.Terrain.Components;
using UnityEngine;

namespace Presentation.Terrain.Systems
{
    /// <summary>
    ///     World-init pipeline step (priority 600). Draws debug rays per hex coloured by terrain level.
    ///     Runs last, after all view geometry exists.
    /// </summary>
    [UsedImplicitly]
    internal sealed class TerrainViewDebugSystem : IPrioritizedUniTaskSystem<MapGenerationStep>
    {
        private const float RayHeight = 5f;
        private const float RayDuration = 5f;

        private readonly World _world;
        private readonly EntitySet _hexSet;

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.TerrainViewDebug;

        public TerrainViewDebugSystem(World world)
        {
            _world = world;
            _hexSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexLevelComponent>()
                .AsSet();
        }

        /// <inheritdoc />
        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (!_world.Has<TerrainViewConfigComponent>())
                return UniTask.CompletedTask;

            var cellSize = _world.Get<TerrainViewConfigComponent>().CellSize;

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

            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _hexSet.Dispose();
        }
    }
}
