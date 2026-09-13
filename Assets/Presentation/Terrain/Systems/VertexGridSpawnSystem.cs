using System.Threading;
using Cysharp.Threading.Tasks;
using Domains.Map.Hex.Utils;
using EcsExtensions;
using JetBrains.Annotations;
using Modules.Boot.Core;
using Presentation.Terrain.Components;
using Presentation.Terrain.Configs;

namespace Presentation.Terrain.Systems
{
    // AppState.InstanceObjects: builds the terrain vertex grid from TerrainViewConfig once, after the configs are
    // loaded and before any map exists.
    [UsedImplicitly]
    public sealed class VertexGridSpawnSystem : IUniTaskSystem
    {
        private readonly EntityStorages _storages;

        public VertexGridSpawnSystem(AppState appState, EntityStorages storages)
        {
            AppState = appState;
            _storages = storages;
        }

        public AppState AppState { get; }

        public UniTask Execute(CancellationToken cancellationToken)
        {
            var terrainViewConfig = _storages.Get<TerrainViewConfig>();
            var vertexGrid = new VertexGrid(terrainViewConfig.CellSize, terrainViewConfig.Subdivisions);
            _storages.Singletons.Set(new VertexGridComponent { Grid = vertexGrid });

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
