using System.Threading;
using Cysharp.Threading.Tasks;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Boot.Core;
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

        private readonly EntityStorages _storages;
        private readonly Archetype _hexSet;

        /// <inheritdoc />
        public int Priority => SystemPriorities.WorldInit.TerrainViewDebug;

        public TerrainViewDebugSystem(EntityStorages storages)
        {
            _storages = storages;
            _hexSet = MapArchetypes.Hex(storages.World);
        }

        /// <inheritdoc />
        public UniTask Update(MapGenerationStep state, CancellationToken cancellationToken)
        {
            if (!_storages.World.HasWorldComponent<TerrainViewConfigComponent>())
                return UniTask.CompletedTask;

            var cellSize = _storages.World.GetWorldComponent<TerrainViewConfigComponent>().CellSize;

            foreach (var hexEntity in _hexSet.Entities)
            {
                var level = hexEntity.GetComponent<HexLevelComponent>().Level;

                Color rayColor;
                if (level < 0)
                    rayColor = Color.blue;
                else if (level == 1)
                    rayColor = Color.green;
                else if (level == 2)
                    rayColor = Color.red;
                else
                    continue;

                var hexCoord = hexEntity.GetComponent<HexIdComponent>().Coords;
                var center = AxialMath.AxialToWorld(hexCoord.Value, cellSize, AxialOrientation.PointyTop);
                Debug.DrawRay(new Vector3(center.x, center.y, center.z), Vector3.up * RayHeight, rayColor, RayDuration);
            }

            return UniTask.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }
}
