using System.Threading;
using Core;
using Cysharp.Threading.Tasks;
using EcsExtensions;
using Friflo.Engine.ECS;
using JetBrains.Annotations;
using Modules.Addressable.Core;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Data;
using Domains.Map.Hex.Tags;
using Presentation.Archetypes;
using Presentation.Terrain.Components;
using Presentation.Terrain.Views;
using Unity.Collections;
using UnityEngine;

namespace Presentation.Terrain.Systems
{
    /// <summary>
    ///     Generates an animated water surface mesh covering all Water-type hexes and
    ///     applies stylistic parameters from <see cref="WaterViewConfigComponent" /> to the renderer.
    ///     Runs after texture generation (Priority 300). Owns the <c>Box&lt;WaterView&gt;</c>; disposes
    ///     the previous view whenever terrain is regenerated.
    /// </summary>
    [UsedImplicitly]
    internal sealed class WaterViewSubSystem : ViewSubSystem
    {
        private const string WATER_VIEW_ADDRESS = "WaterView";

        private readonly IAddressable _addressable;
        private readonly ArchetypeQuery _hexSet;
        private readonly ComponentIndex<HexTypeComponent, HexType> _hexesByType;
        private readonly EntityStore _world;
        private readonly Archetype _waterViewArchetype;

        private Box<WaterView> _waterViewBox;
        private Entity? _waterViewEntity;

        /// <inheritdoc />
        public override int Priority => SystemPriorities.SubSystems.TerrainView.Water;

        /// <param name="world">ECS world used for entity queries and result entity creation.</param>
        /// <param name="addressable">Used to load and instantiate the WaterView prefab.</param>
        public WaterViewSubSystem(EntityStore world, IAddressable addressable)
        {
            _world = world;
            _addressable = addressable;
            _waterViewBox = Box<WaterView>.Empty();
            _hexSet = world.Query<HexIdComponent>().AllTags(Friflo.Engine.ECS.Tags.Get<HexTag>());
            _hexesByType = world.ComponentIndex<HexTypeComponent, HexType>();
            _waterViewArchetype = PresentationArchetypes.WaterView(world);
        }

        /// <inheritdoc />
        public override async UniTask Update(GameState state, CancellationToken cancellationToken)
        {
            DisposeWaterView();

            if (!HasRequiredConfigEntities())
                return;

            var terrainConfig = _world.GetWorldComponent<TerrainViewConfigComponent>();
            var waterConfig = _world.GetWorldComponent<WaterViewConfigComponent>();

            var result = await _addressable.LoadAndInstanceAsync(WATER_VIEW_ADDRESS, cancellationToken);

            if (cancellationToken.IsCancellationRequested || result.Status != Status.Success)
            {
                if (result.Status == Status.Success)
                    result.Box.Dispose();

                return;
            }

            var component = result.Box.Value.GetComponent<WaterView>();
            if (component == null)
            {
                Debug.LogError("[WaterViewSubSystem] WaterView component not found on the loaded prefab.");
                result.Box.Dispose();
                return;
            }

            // Wrap WaterView box so Box.Dispose() cascades to the GameObject instance.
            _waterViewBox = Box<WaterView>.Wrap(component, _ => result.Box.Dispose());

            var hexSize = terrainConfig.CellSize;
            var waterY = -terrainConfig.HeightScale + waterConfig.WaterYOffset;
            var subdivisions = waterConfig.Subdivisions;

            var waterHexes = CollectWaterHexCoords();
            var shoreHexes = CollectShoreHexCoords(waterHexes);
            try
            {
                // Mesh generation is fast (flat mesh, low subdivisions) — runs on main thread.
                // Unity mesh API requires the main thread, so no background offload is needed here.
                component.Generate(waterHexes, shoreHexes, hexSize, waterY, subdivisions);
            }
            finally
            {
                waterHexes.Dispose();
                shoreHexes.Dispose();
            }

            component.ApplyConfig(in waterConfig);

            DestroyWaterViewEntity();
            var entity = _waterViewArchetype.CreateEntity();
            entity.AddComponent(new WaterViewComponent { ObjectRef = component });
            _waterViewEntity = entity;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            DisposeWaterView();
            base.Dispose();
        }

        /// <summary>
        ///     Collects all Water-type hex coords into a
        ///     <see cref="NativeHashSet{T}" />. Caller must dispose the returned set.
        /// </summary>
        private NativeHashSet<HexCoord> CollectWaterHexCoords()
        {
            var waterHexes = new NativeHashSet<HexCoord>(1, Allocator.Persistent);

            foreach (var entity in _hexesByType[HexType.Water])
                waterHexes.Add(entity.GetComponent<HexIdComponent>().Coords);

            return waterHexes;
        }

        /// <summary>
        ///     Collects all hex entities that are adjacent to at least one water hex but are not
        ///     Water themselves (the shoreline ring).
        ///     Caller must dispose the returned set.
        /// </summary>
        /// <param name="waterHexes">Already-computed water hex set; not modified.</param>
        private NativeHashSet<HexCoord> CollectShoreHexCoords(NativeHashSet<HexCoord> waterHexes)
        {
            var entities   = _hexSet.Entities;
            var shoreHexes = new NativeHashSet<HexCoord>(
                Mathf.Max(1, entities.Count),
                Allocator.Persistent);

            foreach (var entity in entities)
            {
                if (!entity.HasComponent<HexTypeComponent>() || entity.GetComponent<HexTypeComponent>().Type == HexType.Water)
                    continue;

                var coord = entity.GetComponent<HexIdComponent>().Coords;
                for (var d = 0; d < AxialMath.NeighborCount; d++)
                {
                    var neighbor = coord + AxialMath.NeighborsPointyTop[d];
                    if (waterHexes.Contains(neighbor))
                    {
                        shoreHexes.Add(coord);
                        break;
                    }
                }
            }

            return shoreHexes;
        }

        /// <summary>Validates that all singleton config entity sets are populated.</summary>
        private bool HasRequiredConfigEntities()
        {
            if (_world.HasWorldComponent<TerrainViewConfigComponent>() && _world.HasWorldComponent<WaterViewConfigComponent>())
                return true;

            Debug.LogError("[WaterViewSubSystem] One or more required configs are missing.");
            return false;
        }

        private void DisposeWaterView()
        {
            DestroyWaterViewEntity();
            DisposeWaterViewBox();
        }

        private void DestroyWaterViewEntity()
        {
            if (_waterViewEntity == null || _waterViewEntity.Value.IsNull)
                return;

            _waterViewEntity.Value.DeleteEntity();
            _waterViewEntity = null;
        }

        private void DisposeWaterViewBox()
        {
            if (!_waterViewBox.Exist)
                return;

            _waterViewBox.Dispose();
            _waterViewBox = Box<WaterView>.Empty();
        }
    }
}
