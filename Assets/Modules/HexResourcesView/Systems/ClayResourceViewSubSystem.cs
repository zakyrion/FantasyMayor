using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.HexCore.Components;
using Modules.HexResources.Data;
using Modules.HexResourcesView.Components;
using Modules.HexResourcesView.Helpers;
using Modules.TerrainView.Components;
using Unity.Collections;
using UnityEngine;

namespace Modules.HexResourcesView.Systems
{
    /// <summary>
    ///     One-shot pipeline subsystem (priority 200 within <see cref="HexResourcesViewSystem" />, which
    ///     itself runs at 400 — after the terrain view at 300). For every clay hex it sinks an organic
    ///     depression into the shared <see cref="Modules.HexesCore.Utils.VertexGrid" /> and paints a clay
    ///     gradient into the persistent terrain texture, both masked by the same <see cref="ClayFootprint" />.
    ///     After all hexes are processed it re-applies the grid heights to the mesh once and uploads the
    ///     texture once. Clay is persistent, so this never needs to be reverted.
    /// </summary>
    [UsedImplicitly]
    internal sealed class ClayResourceViewSubSystem : HexResourcesViewSubSystem
    {
        private const int ExecutionPriority = 200;

        private readonly World _world;
        private readonly EntitySet _hexSet;
        private readonly ClayGroundPainter _painter = new();

        private readonly ClayDepressionShaper _shaper = new();
        private readonly EntitySet _terrainViewSet;
        private readonly EntitySet _textureSet;

        public override int Priority => ExecutionPriority;
        protected override ResourceType TargetResourceType => ResourceType.Clay;

        public ClayResourceViewSubSystem(World world)
            : base(world)
        {
            _world = world;
            _textureSet = world.GetEntities().With<TerrainTextureComponent>().AsSet();
            _terrainViewSet = world.GetEntities().With<TerrainViewComponent>().AsSet();
            _hexSet = world.GetEntities().With<HexIdComponent>().AsSet();
        }

        public override void Update(GameState state)
        {
            var clayEntities = GetTargetResourceEntities();
            if (clayEntities.Length == 0)
                return;

            if (!TryGetVertexGrid(out var grid) || !_world.Has<ClayViewConfigComponent>())
                return;

            if (!_world.Has<TerrainViewConfigComponent>() || _textureSet.Count == 0 || _terrainViewSet.Count == 0)
            {
                Debug.LogWarning("[ClayResourceViewSubSystem] Missing terrain config, texture, or view — clay skipped.");
                return;
            }

            var clayConfig = _world.Get<ClayViewConfigComponent>();
            var cellSize = _world.Get<TerrainViewConfigComponent>().CellSize;
            var texture = _textureSet.GetEntities()[0].Get<TerrainTextureComponent>().Texture;
            var terrainView = _terrainViewSet.GetEntities()[0].Get<TerrainViewComponent>().ObjectRef;

            if (texture == null || terrainView == null)
            {
                Debug.LogWarning("[ClayResourceViewSubSystem] Terrain texture or view reference is null — clay skipped.");
                return;
            }

            var radius = clayConfig.DepressionRadius * cellSize;
            if (radius <= 0f)
                return; // clay config not authored yet — nothing to deform or paint

            InitializePainter(texture, cellSize);

            foreach (var clayEntity in clayEntities)
            {
                var hex = clayEntity.Get<HexIdComponent>().Coords;
                var centerXZ = AxialMath.AxialToWorld2D(hex.Value, cellSize);
                var footprint = new ClayFootprint(
                    hex,
                    radius,
                    clayConfig.FootprintAspect,
                    clayConfig.PearFactor,
                    clayConfig.NoiseAmplitude,
                    clayConfig.NoiseFrequency);

                _shaper.Shape(grid, hex, footprint, centerXZ, clayConfig.DepressionDepth);
                _painter.PaintClay(footprint, centerXZ, clayConfig.ClayCenterColor, clayConfig.ClayRimColor);
            }

            terrainView.ApplyHeightsFromVertexGrid(grid);
            _painter.Apply();
        }

        public override void Dispose()
        {
            _textureSet.Dispose();
            _terrainViewSet.Dispose();
            _hexSet.Dispose();
            base.Dispose();
        }

        private void InitializePainter(Texture2D texture, float cellSize)
        {
            var hexEntities = _hexSet.GetEntities();
            var hexCoords = new NativeArray<HexCoord>(hexEntities.Length, Allocator.Temp);
            for (var i = 0; i < hexEntities.Length; i++)
                hexCoords[i] = hexEntities[i].Get<HexIdComponent>().Coords;

            _painter.Initialize(hexCoords, texture, cellSize);
            hexCoords.Dispose();
        }
    }
}
