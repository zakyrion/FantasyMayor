using System;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Domains.Map.HexResources.Data;
using Presentation.HexResources.Components;
using Presentation.HexResources.Helpers;
using Presentation.Terrain.Components;
using Unity.Collections;
using UnityEngine;

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
        private readonly ArchetypeQuery _hexSet;
        private readonly EntityStore _world;

        private UnityEngine.Transform _root;

        public override int Priority => SystemPriorities.SubSystems.HexResourceView.Forest;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Forest;

        public ForestHexResourceViewSubSystem(EntityStore world)
            : base(world)
        {
            _world = world;
            _hexSet = world.Query<HexIdComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexTag>());
        }

        public override void Update(GameState state)
        {
            var forestEntities = GetTargetResourceEntities();
            if (forestEntities.Length == 0)
                return;

            if (!TryGetVertexGrid(out var vertexGrid))
                throw new InvalidOperationException(
                    "ForestHexResourceViewSubSystem: VertexGridComponent world component is missing.");

            if (!_world.HasWorldComponent<TerrainViewConfigComponent>() || !_world.HasWorldComponent<HexResourcesViewConfigComponent>() || !_world.HasWorldComponent<TerrainTextureComponent>())
                return;

            var texture = _world.GetWorldComponent<TerrainTextureComponent>().Texture;
            if (texture == null)
                return;

            var viewConfig = _world.GetWorldComponent<HexResourcesViewConfigComponent>().Value;
            var cellSize = _world.GetWorldComponent<TerrainViewConfigComponent>().CellSize;

            if (_root == null)
                _root = new GameObject("ForestViewRoot").transform;

            var splats = new NativeList<ForestGroundPainter.Splat>(64, Allocator.Temp);
            var planter = new ForestPlanter();

            foreach (var forestEntity in forestEntities)
            {
                var hex = forestEntity.GetComponent<HexIdFKComponent>().Coords;
                planter.PlantHex(_world, _root, hex, vertexGrid, viewConfig, ref splats);
            }

            // Append-only: paint the new patches over the current pixels, once.
            planter.Paint(_hexSet, cellSize, splats, texture);
            splats.Dispose();
        }

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
