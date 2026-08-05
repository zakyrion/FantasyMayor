using Domains.Map.Archetypes;
using Domains.Map.Hex.Components;
using Domains.Map.HexResources.Data;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Presentation.Archetypes;
using Presentation.HexResources.Components;
using Presentation.HexResources.Helpers;
using Presentation.Terrain.Components;
using Unity.Collections;
using UnityEngine;

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
        private readonly EntityStorages _storages;
        private readonly Archetype _hexSet;
        private readonly ClayGroundPainter _painter = new();

        private readonly ClayDepressionShaper _shaper = new();
        private readonly Archetype _terrainViewSet;

        public override int Priority => SystemPriorities.SubSystems.HexResourceView.Clay;
        protected override HexResourceType TargetHexResourceType => HexResourceType.Clay;

        public ClayHexResourceViewSubSystem(EntityStorages storages)
            : base(storages.World)
        {
            _storages = storages;
            _terrainViewSet = PresentationArchetypes.TerrainView(storages.World);
            _hexSet = MapArchetypes.Hex(storages.World);
        }

        public override void Update(GameState state)
        {
            var clayEntities = GetTargetResourceEntities();
            if (clayEntities.Length == 0)
                return;

            if (!TryGetVertexGrid(out var grid) || !_storages.World.HasWorldComponent<ClayViewConfigComponent>())
                return;

            if (!_storages.World.HasWorldComponent<TerrainViewConfigComponent>() || !_storages.World.HasWorldComponent<TerrainTextureComponent>() || _terrainViewSet.Count == 0)
            {
                Debug.LogWarning("[ClayHexResourceViewSubSystem] Missing terrain config, texture, or view — clay skipped.");
                return;
            }

            var clayConfig = _storages.World.GetWorldComponent<ClayViewConfigComponent>();
            var cellSize = _storages.World.GetWorldComponent<TerrainViewConfigComponent>().CellSize;
            var texture = _storages.World.GetWorldComponent<TerrainTextureComponent>().Texture;
            _terrainViewSet.TryGetFirst(out var terrainViewEntity);
            var terrainView = terrainViewEntity.GetComponent<TerrainViewComponent>().ObjectRef;

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
                var hex = clayEntity.GetComponent<HexIdFKComponent>().Coords;
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
            base.Dispose();
        }

        private void InitializePainter(Texture2D texture, float cellSize)
        {
            var hexEntities = _hexSet.Entities;
            var hexCoords = new NativeArray<HexCoord>(hexEntities.Count, Allocator.Temp);
            var index = 0;
            foreach (var hexEntity in hexEntities)
                hexCoords[index++] = hexEntity.GetComponent<HexIdComponent>().Coords;

            _painter.Initialize(hexCoords, texture, cellSize);
            hexCoords.Dispose();
        }
    }
}
