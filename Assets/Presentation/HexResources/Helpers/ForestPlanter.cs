using Friflo.Engine.ECS;
using Modules.AxialSystem;
using Domains.Map.Hex.Components;
using Domains.Map.Hex.Utils;
using Domains.Map.HexResources.Data;
using Presentation.HexResources.Components;
using Presentation.HexResources.Configs;
using Presentation.HexResources.Data;
using Presentation.HexResources.Views;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Presentation.HexResources.Tags;

namespace Presentation.HexResources.Helpers
{
    /// <summary>
    ///     Stateless tree placer shared by the one-shot startup spawn (<see cref="Systems.ForestHexResourceViewSubSystem" />)
    ///     and the reactive runtime spawn (<see cref="Systems.ForestSpawnSystem" />). Plants a hex's trees as
    ///     view entities (<see cref="HexIdFKComponent" /> + <see cref="ForestViewComponent" />), parents the
    ///     prefab instances under <paramref name="root" />, and appends each instance's green-ground splat to
    ///     the caller's batch (the caller paints once after planting all hexes — paint is append-only).
    ///     Placement iterates shuffled owned-vertex positions and rejects any vertex closer than
    ///     <c>entryRadius + placedRadius</c> — per-prefab radii, not a global minimum distance.
    /// </summary>
    public sealed class ForestPlanter
    {
        private const int MaxTrees = 7;
        private const int MinTrees = 5;

        public void PlantHex(
            EntityStore world,
            UnityEngine.Transform root,
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
                var instance = Object.Instantiate(entry.Prefab, worldPos, rotation, root);
                instance.transform.localScale = Vector3.one * scale;

                var view = instance.GetComponent<ForestView>();
                if (view == null)
                    Debug.LogWarning($"[ForestPlanter] Prefab '{entry.Prefab.name}' is missing ForestView component.");

                var viewEntity = world.CreateEntity();
                viewEntity.AddComponent(new HexIdFKComponent { Coords = hex });
                viewEntity.AddComponent(new ForestViewComponent { Type = HexResourceType.Forest, View = view });
                viewEntity.AddTag<ForestViewTag>();

                if (entry.GroundTint.a > 0f)
                    splats.Add(new ForestGroundPainter.Splat(worldPos, entry.Radius, entry.GroundTint));

                placed.Add(new Placement(position, entry.Radius));
            }

            placed.Dispose();
            owned.Dispose();
        }

        /// <summary>
        ///     Paints a batch of freshly planted splats into the terrain texture. No-op for an empty batch.
        ///     The square UV rect is a pure function of the (fixed) hex set, recomputed on the rare paint
        ///     delta rather than cached. Append-only — blends over current pixels, never reverts.
        /// </summary>
        public void Paint(
            ArchetypeQuery hexSet,
            float cellSize,
            NativeList<ForestGroundPainter.Splat> splats,
            Texture2D texture)
        {
            if (splats.Length == 0)
                return;

            var hexEntities = hexSet.Entities;
            var hexCoords = new NativeArray<HexCoord>(hexEntities.Count, Allocator.Temp);
            var index = 0;
            foreach (var hexEntity in hexEntities)
                hexCoords[index++] = hexEntity.GetComponent<HexIdComponent>().Coords;

            var uv = ForestGroundPainter.ComputeUvRect(hexCoords, cellSize);
            hexCoords.Dispose();

            ForestGroundPainter.Paint(splats, texture, in uv);
        }

        private bool Overlaps(NativeList<Placement> placed, float3 position, float radius)
        {
            for (var i = 0; i < placed.Length; i++)
                if (math.distance(position, placed[i].Position) < radius + placed[i].Radius)
                    return true;

            return false;
        }

        private void ShufflePartial(NativeList<VertexCoord> list, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var j = Random.Range(i, list.Length);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Picks a random usable forest entry from the config without allocating.</summary>
        private bool TryPickForestEntry(HexResourcesViewConfig viewConfig, out HexResourcesViewConfigEntry entry)
        {
            var resources = viewConfig.Resources;

            var count = 0;
            for (var i = 0; i < resources.Length; i++)
                if (resources[i].Type == HexResourceType.Forest && resources[i].Prefab != null)
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
                if (resources[i].Type != HexResourceType.Forest || resources[i].Prefab == null)
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
