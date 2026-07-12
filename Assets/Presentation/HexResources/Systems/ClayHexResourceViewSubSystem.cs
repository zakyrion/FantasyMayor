using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Tags;
using Domains.Map.HexResources.Data;
using Presentation.HexResources.Components;
using Presentation.HexResources.Helpers;
using Presentation.Terrain.Components;
using Unity.Collections;
using UnityEngine;
using Presentation.Terrain.Tags;

namespace Presentation.HexResources.Systems
{
    /// <summary>
    ///     One-shot pipeline subsystem (priority 200 within <see cref="HexResourcesViewSystem" />, which
    ///     itself runs at 400 — after the terrain view at 300). For every clay hex it sinks an organic
    ///     depression into the shared <see cref="Domains.Map.Hex.Utils.VertexGrid" /> and paints a clay
    ///     gradient into the persistent terrain texture, both masked by the same <see cref="ClayFootprint" />.
    ///     After all hexes are processed it re-applies the grid heights to the mesh once and uploads the
    ///     texture once. Clay is persistent, so this never needs to be reverted.
    /// </summary>
    [UsedImplicitly]
    internal sealed class ClayHexResourceViewSubSystem : HexResourcesViewSubSystem
    {
        private readonly World _world;
        private readonly EntitySet _hexSet;
        private readonly ClayGroundPainter _painter = new();

        private readonly ClayDepressionShaper _shaper = new();
        private readonly EntitySet _terrainViewSet;

        public override int Priority => SystemPriorities.SubSystems.HexResourceView.Clay;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Clay;

        public ClayHexResourceViewSubSystem(World world)
            : base(world)
        {
            _world = world;
            _terrainViewSet = world.GetEntities().With<TerrainViewComponent>().With<TerrainViewTag>().AsSet();
            _hexSet = world.GetEntities().With<HexIdComponent>().With<HexTag>().AsSet();
        }

        public override void Update(GameState state)
        {
            var clayEntities = GetTargetResourceEntities();
            if (clayEntities.Length == 0)
                return;

            if (!TryGetVertexGrid(out var grid) || !_world.Has<ClayViewConfigComponent>())
                return;

            if (!_world.Has<TerrainViewConfigComponent>() || !_world.Has<TerrainTextureComponent>() || _terrainViewSet.Count == 0)
            {
                Debug.LogWarning("[ClayHexResourceViewSubSystem] Missing terrain config, texture, or view — clay skipped.");
                return;
            }

            var clayConfig = _world.Get<ClayViewConfigComponent>();
            var cellSize = _world.Get<TerrainViewConfigComponent>().CellSize;
            var texture = _world.Get<TerrainTextureComponent>().Texture;
            var terrainView = _terrainViewSet.GetEntities()[0].Get<TerrainViewComponent>().ObjectRef;

            if (texture == null || terrainView == null)
            {
                Debug.LogWarning("[ClayHexResourceViewSubSystem] Terrain texture or view reference is null — clay skipped.");
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
