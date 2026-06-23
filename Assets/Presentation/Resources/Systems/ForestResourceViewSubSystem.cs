using System;
using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Domains.Map.HexResources.Data;
using Presentation.Resources.Components;
using Presentation.Resources.Helpers;
using Presentation.Terrain.Components;
using Unity.Collections;
using UnityEngine;

namespace Presentation.Resources.Systems
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
    internal sealed class ForestResourceViewSubSystem : HexResourcesViewSubSystem
    {
        private const int ExecutionPriority = 400;

        private readonly EntitySet _hexSet;
        private readonly World _world;

        private Transform _root;

        public override int Priority => ExecutionPriority;
        protected override ResourceType TargetResourceType => ResourceType.Forest;

        public ForestResourceViewSubSystem(World world)
            : base(world)
        {
            _world = world;
            _hexSet = world.GetEntities().With<HexTag>().With<HexIdComponent>().AsSet();
        }

        public override void Update(GameState state)
        {
            var forestEntities = GetTargetResourceEntities();
            if (forestEntities.Length == 0)
                return;

            if (!TryGetVertexGrid(out var vertexGrid))
                throw new InvalidOperationException(
                    "ForestResourceViewSubSystem: VertexGridComponent world component is missing.");

            if (!_world.Has<TerrainViewConfigComponent>() || !_world.Has<HexResourcesViewConfigComponent>() || !_world.Has<TerrainTextureComponent>())
                return;

            var texture = _world.Get<TerrainTextureComponent>().Texture;
            if (texture == null)
                return;

            var viewConfig = _world.Get<HexResourcesViewConfigComponent>().Value;
            var cellSize = _world.Get<TerrainViewConfigComponent>().CellSize;

            if (_root == null)
                _root = new GameObject("ForestViewRoot").transform;

            var splats = new NativeList<ForestGroundPainter.Splat>(64, Allocator.Temp);
            var planter = new ForestPlanter();

            foreach (var forestEntity in forestEntities)
            {
                var hex = forestEntity.Get<HexIdComponent>().Coords;
                planter.PlantHex(_world, _root, hex, vertexGrid, viewConfig, ref splats);
            }

            // Append-only: paint the new patches over the current pixels, once.
            planter.Paint(_hexSet, cellSize, splats, texture);
            splats.Dispose();
        }

        public override void Dispose()
        {
            _hexSet.Dispose();
            base.Dispose();
        }
    }
}
