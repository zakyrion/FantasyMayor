using DefaultEcs;
using DefaultECSExtensions;
using JetBrains.Annotations;
using Modules.AxialSystem;
using Modules.HexCore.Components;
using Modules.HexResources.Components;
using Modules.HexResources.Data;
using Modules.HexResourcesView.Components;
using Modules.HexResourcesView.Configs;
using Modules.HexResourcesView.Data;
using Modules.HexResourcesView.Helpers;
using Modules.HexResourcesView.Views;
using Modules.HexesCore.Utils;
using Modules.TerrainView.Components;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Modules.HexResourcesView.Systems
{
    /// <summary>
    ///     Reactive per-frame maintainer of the forest view. The ECS world is the single source of truth:
    ///     each tree is a view entity (<see cref="HexIdComponent" /> + <see cref="ForestViewComponent" />),
    ///     and this system holds no mirror state. Every frame it diffs the forest resource hexes against the
    ///     forest-view hexes: an appeared hex gets trees plus a one-shot green ground splat; a vanished hex has
    ///     its tree entities (and GameObjects) destroyed — the ground paint is left in place (a chopped forest
    ///     leaves vegetated ground, not bare terrain), so painting is append-only and no baseline is kept.
    ///     Per-frame work uses `Unity.Collections` (`Allocator.Temp`), disposed within the frame. Same diffing
    ///     idiom as `TerrainView`'s `HexSelectionViewSystem`. Gated by the persistent
    ///     <see cref="TerrainTextureComponent" /> — the paint target and readiness signal.
    /// </summary>
    [UsedImplicitly]
    public sealed class ForestViewSyncSystem : UpdatedSystem
    {
        // No hard ordering dependency on other per-frame systems; placed after the view systems
        // (HexSelectionViewSystem = 501) and well before EventCleanupSystem (int.MaxValue).
        private const int ExecutionPriority = 600;
        private const int MaxTrees = 7;
        private const int MinTrees = 5;

        private readonly EntitySet _forestViewSet;
        private readonly EntitySet _hexSet;
        private readonly EntitySet _resourceSet;
        private readonly EntitySet _vertexGridSet;

        private readonly World _world;

        private Transform _root;

        public override int Priority => ExecutionPriority;

        public ForestViewSyncSystem(World world)
            : base(world.GetEntities()
                .With<TerrainTextureComponent>()
                .AsSet())
        {
            _world = world;
            _resourceSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<HexResourcesComponent>()
                .AsSet();

            _forestViewSet = world.GetEntities()
                .With<HexIdComponent>()
                .With<ForestViewComponent>()
                .AsSet();

            _vertexGridSet = world.GetEntities().With<VertexGridComponent>().AsSet();
            _hexSet = world.GetEntities().With<HexIdComponent>().AsSet();
        }

        protected override void Update(GameState state, in Entity textureEntity)
        {
            var texture = textureEntity.Get<TerrainTextureComponent>().Texture;
            if (texture == null)
                return;

            if (_vertexGridSet.Count == 0 || !_world.Has<TerrainViewConfigComponent>() || !_world.Has<HexResourcesViewConfigComponent>())
                return;

            var vertexGrid = _vertexGridSet.GetEntities()[0].Get<VertexGridComponent>().Grid;
            var viewConfig = _world.Get<HexResourcesViewConfigComponent>().Value;

            if (_root == null)
                _root = new GameObject("ForestViewRoot").transform;

            var currentForestHexes = new NativeHashSet<HexCoord>(64, Allocator.Temp);
            var viewedHexes = new NativeHashSet<HexCoord>(64, Allocator.Temp);
            var staleViews = new NativeList<Entity>(8, Allocator.Temp);
            var newSplats = new NativeList<ForestGroundPainter.Splat>(64, Allocator.Temp);

            // Forested hexes wanted this frame.
            foreach (ref readonly var entity in _resourceSet.GetEntities())
                if (entity.Get<HexResourcesComponent>().Type == ResourceType.Forest)
                    currentForestHexes.Add(entity.Get<HexIdComponent>().Coords);

            // Single pass over the view entities: record every viewed hex, flag those no longer forested.
            foreach (ref readonly var entity in _forestViewSet.GetEntities())
            {
                var hex = entity.Get<HexIdComponent>().Coords;
                viewedHexes.Add(hex);

                if (!currentForestHexes.Contains(hex))
                    staleViews.Add(entity);
            }

            // Appeared: forested but not yet viewed -> spawn trees and collect their ground splats.
            foreach (var hex in currentForestHexes)
            {
                if (viewedHexes.Contains(hex))
                    continue;

                SpawnHex(hex, vertexGrid, viewConfig, ref newSplats);
            }

            // Vanished: viewed but no longer forested -> destroy trees. Ground paint stays (former forest floor).
            for (var i = 0; i < staleViews.Length; i++)
                DestroyView(staleViews[i]);

            // Append-only: paint just the new patches over the current pixels; vanished patches are never reverted.
            PaintGround(newSplats, texture);

            currentForestHexes.Dispose();
            viewedHexes.Dispose();
            staleViews.Dispose();
            newSplats.Dispose();
        }

        public override void Dispose()
        {
            _resourceSet.Dispose();
            _forestViewSet.Dispose();
            _vertexGridSet.Dispose();
            _hexSet.Dispose();
            base.Dispose();
        }

        private void DestroyView(Entity entity)
        {
            var view = entity.Get<ForestViewComponent>().View;
            if (view != null)
                Object.Destroy(view.gameObject);

            if (entity.IsAlive)
                entity.Dispose();
        }

        private bool Overlaps(NativeList<Placement> placed, float3 position, float radius)
        {
            for (var i = 0; i < placed.Length; i++)
                if (math.distance(position, placed[i].Position) < radius + placed[i].Radius)
                    return true;

            return false;
        }

        /// <summary>
        ///     Paints the freshly spawned patches into the terrain texture. No-op for an empty batch. The
        ///     square UV rect is a pure function of the (fixed) hex set, recomputed here on the rare paint
        ///     delta rather than cached.
        /// </summary>
        private void PaintGround(NativeList<ForestGroundPainter.Splat> splats, Texture2D texture)
        {
            if (splats.Length == 0)
                return;

            var hexEntities = _hexSet.GetEntities();
            var hexCoords = new NativeArray<HexCoord>(hexEntities.Length, Allocator.Temp);
            for (var i = 0; i < hexEntities.Length; i++)
                hexCoords[i] = hexEntities[i].Get<HexIdComponent>().Coords;

            var hexSize = _world.Get<TerrainViewConfigComponent>().CellSize;
            var uv = ForestGroundPainter.ComputeUvRect(hexCoords, hexSize);
            hexCoords.Dispose();

            ForestGroundPainter.Paint(splats.AsArray(), texture, in uv);
        }

        private void ShufflePartial(NativeList<VertexCoord> list, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var j = Random.Range(i, list.Length);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void SpawnHex(
            HexCoord hex,
            VertexGrid vertexGrid,
            HexResourcesViewConfig viewConfig,
            ref NativeList<ForestGroundPainter.Splat> splats)
        {
            var owned = new NativeList<VertexCoord>(32, Allocator.Temp);
            foreach (var vc in vertexGrid.GetOwnedVertexCoords(hex))
                owned.Add(vc);

            if (owned.Length == 0)
            {
                owned.Dispose();
                return;
            }

            ShufflePartial(owned, owned.Length);

            var maxTrees = math.min(Random.Range(MinTrees, MaxTrees + 1), owned.Length);
            var placed = new NativeList<Placement>(maxTrees, Allocator.Temp);

            for (var i = 0; i < owned.Length; i++)
            {
                if (placed.Length >= maxTrees)
                    break;

                if (!TryPickForestEntry(viewConfig, out var entry))
                    break;

                var position = vertexGrid.Get(owned[i]).Position;
                if (Overlaps(placed, position, entry.Radius))
                    continue;

                var worldPos = new Vector3(position.x, position.y, position.z);
                var scale = Random.Range(entry.ScaleRange.x, entry.ScaleRange.y);
                var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var instance = Object.Instantiate(entry.Prefab, worldPos, rotation, _root);
                instance.transform.localScale = Vector3.one * scale;

                var view = instance.GetComponent<ForestView>();
                if (view == null)
                    Debug.LogWarning($"[ForestViewSyncSystem] Prefab '{entry.Prefab.name}' is missing ForestView component.");

                var viewEntity = _world.CreateEntity();
                viewEntity.Set(new HexIdComponent { Coords = hex });
                viewEntity.Set(new ForestViewComponent { Type = ResourceType.Forest, View = view });

                if (entry.GroundTint.a > 0f)
                    splats.Add(new ForestGroundPainter.Splat(worldPos, entry.Radius, entry.GroundTint));

                placed.Add(new Placement(position, entry.Radius));
            }

            placed.Dispose();
            owned.Dispose();
        }

        /// <summary>Picks a random usable forest entry from the config without allocating.</summary>
        private bool TryPickForestEntry(HexResourcesViewConfig viewConfig, out HexResourcesViewConfigEntry entry)
        {
            var resources = viewConfig.Resources;

            var count = 0;
            for (var i = 0; i < resources.Length; i++)
                if (resources[i].Type == ResourceType.Forest && resources[i].Prefab != null)
                    count++;

            if (count == 0)
            {
                entry = default;
                return false;
            }

            var pick = Random.Range(0, count);
            var seen = 0;
            for (var i = 0; i < resources.Length; i++)
            {
                if (resources[i].Type != ResourceType.Forest || resources[i].Prefab == null)
                    continue;

                if (seen == pick)
                {
                    entry = resources[i];
                    return true;
                }

                seen++;
            }

            entry = default;
            return false;
        }

        private readonly struct Placement
        {
            public readonly float3 Position;
            public readonly float Radius;

            public Placement(float3 position, float radius)
            {
                Position = position;
                Radius = radius;
            }
        }
    }
}
