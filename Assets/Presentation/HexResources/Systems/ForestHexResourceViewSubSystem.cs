using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Presentation.HexResources.Components;
using Presentation.HexResources.Helpers;
using Presentation.Terrain.Components;
using Unity.Collections;
using UnityEngine;
using Presentation.HexResources.Configs;
using Presentation.Terrain.Configs;

namespace Presentation.HexResources.Systems
{
    /// <summary>
    ///     One-shot pipeline subsystem (priority 400 within <see cref="HexResourcesViewSystem" /> at 400 —
    ///     after Clay 200 and Fish 300, so forest ground splats land on top). Builds the whole forest view
    ///     once at map creation: for every forest resource hex it plants trees (via <see cref="ForestPlanter" />)
    ///     and collects their green-ground splats, then paints the batch into the persistent terrain texture
    ///     once. Runtime spawn/despawn is reactive and lives in <see cref="ForestSpawnSystem" /> /
    ///     <see cref="ForestDespawnSystem" /> (Gameplay) — this subsystem does only the startup bulk.
    /// </summary>
    [UsedImplicitly]
    internal sealed class ForestHexResourceViewSubSystem : HexResourcesViewSubSystem
    {
        private readonly Archetype _hexSet;
        private readonly EntityStorages _storages;

        private UnityEngine.Transform _root;

        public override int Priority => SystemPriorities.SubSystems.HexResourceView.Forest;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Forest;

        public ForestHexResourceViewSubSystem(EntityStorages storages)
            : base(storages)
        {
            _storages = storages;
            _hexSet = MapArchetypes.Hex(storages.World);
        }

        public override UniTask Update(CancellationToken cancellationToken)
        {
            var forestEntities = GetTargetResourceEntities();
            if (forestEntities.Length == 0)
                return UniTask.CompletedTask;

            if (!TryGetVertexGrid(out var vertexGrid))
                throw new InvalidOperationException(
                    "ForestHexResourceViewSubSystem: VertexGridComponent singleton component is missing.");

            if (!_storages.Singletons.Has<TerrainTextureComponent>())
                return UniTask.CompletedTask;

            var texture = _storages.Singletons.Get<TerrainTextureComponent>().Texture;
            if (texture == null)
                return UniTask.CompletedTask;

            var viewConfig = _storages.Get<HexResourcesViewConfig>();
            var cellSize = _storages.Get<TerrainViewConfig>().CellSize;

            if (_root == null)
                _root = new GameObject("ForestViewRoot").transform;

            var splats = new NativeList<ForestGroundPainter.Splat>(64, Allocator.Temp);
            var planter = new ForestPlanter();

            foreach (var forestEntity in forestEntities)
            {
                var hex = forestEntity.GetComponent<HexIdFKComponent>().Coords;
                planter.PlantHex(_storages.World, _root, hex, vertexGrid, viewConfig, ref splats);
            }

            // Append-only: paint the new patches over the current pixels, once.
            planter.Paint(_hexSet, cellSize, splats, texture);
            splats.Dispose();

            return UniTask.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
