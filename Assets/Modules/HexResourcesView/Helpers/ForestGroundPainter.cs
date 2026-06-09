using Modules.AxialSystem;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Modules.HexResourcesView.Helpers
{
    /// <summary>
    ///     Stateless painter for "forest ground" patches. Blends soft-edged colored circles beneath placed
    ///     trees directly into the persistent terrain <see cref="Texture2D" />. Painting is append-only: a
    ///     patch is splatted once when its hex appears and is never reverted — a chopped forest leaves
    ///     vegetated ground, not bare terrain, so no baseline snapshot is kept. The world-to-pixel mapping
    ///     duplicates the one used by <see cref="TerrainViewTextureSubSystem" /> so splats land in the same UV
    ///     space as the baked texture.
    /// </summary>
    internal static class ForestGroundPainter
    {
        /// <summary>A single ground patch request: a colored circle in world space.</summary>
        internal readonly struct Splat
        {
            public readonly Vector3 WorldPosition;
            public readonly float Radius;
            public readonly Color Tint;

            public Splat(Vector3 worldPosition, float radius, Color tint)
            {
                WorldPosition = worldPosition;
                Radius = radius;
                Tint = tint;
            }
        }

        /// <summary>The square UV rect splats map into. A pure function of the hex set; holds no pixels.</summary>
        internal readonly struct UvRect
        {
            public readonly float2 Min;
            public readonly float Size;

            public UvRect(float2 min, float size)
            {
                Min = min;
                Size = size;
            }
        }

        /// <summary>
        ///     Blends every splat into the texture's CURRENT pixels (terrain plus any earlier forest paint)
        ///     and uploads once. No-op for an empty batch. Called only on the appeared-delta, never per frame.
        /// </summary>
        public static void Paint(NativeArray<Splat> splats, Texture2D texture, in UvRect uv)
        {
            if (splats.Length == 0 || uv.Size <= 0f)
                return;

            var resolution = texture.width;
            var pixels = texture.GetPixels32();

            for (var i = 0; i < splats.Length; i++)
                ApplySplat(splats[i], pixels, resolution, uv);

            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        /// <summary>
        ///     Computes the square UV rect from hex centers plus their six pointy-top corners. Pure.
        ///     Mirrors <see cref="TerrainViewTextureSubSystem" />.ComputeSquareUVRect so vertex positions map
        ///     to the identical UV space as the baked texture.
        /// </summary>
        public static UvRect ComputeUvRect(NativeArray<HexCoord> hexCoords, float hexSize)
        {
            var minX = float.MaxValue;
            var minZ = float.MaxValue;
            var maxX = float.MinValue;
            var maxZ = float.MinValue;

            foreach (var hexCoord in hexCoords)
            {
                var center = AxialMath.AxialToWorld2D(hexCoord.Value, hexSize);

                if (center.x < minX) minX = center.x;
                if (center.x > maxX) maxX = center.x;
                if (center.y < minZ) minZ = center.y;
                if (center.y > maxZ) maxZ = center.y;

                for (var i = 0; i < 6; i++)
                {
                    var angle = math.radians(60f * i + 30f);
                    var cx = center.x + hexSize * math.cos(angle);
                    var cz = center.y + hexSize * math.sin(angle);

                    if (cx < minX) minX = cx;
                    if (cx > maxX) maxX = cx;
                    if (cz < minZ) minZ = cz;
                    if (cz > maxZ) maxZ = cz;
                }
            }

            var size = math.max(maxX - minX, maxZ - minZ);
            var centerX = (minX + maxX) * 0.5f;
            var centerZ = (minZ + maxZ) * 0.5f;
            return new UvRect(new float2(centerX - size * 0.5f, centerZ - size * 0.5f), size);
        }

        /// <summary>
        ///     Blends one colored circle into <paramref name="pixels" /> with linear distance falloff.
        ///     Falloff weight is scaled by the tint alpha, so alpha acts as the maximum patch strength.
        /// </summary>
        private static void ApplySplat(in Splat splat, Color32[] pixels, int resolution, in UvRect uv)
        {
            var center = WorldToPixel(new float2(splat.WorldPosition.x, splat.WorldPosition.z), resolution, uv);
            var radiusPx = splat.Radius / uv.Size * (resolution - 1);
            if (radiusPx < 1f)
                radiusPx = 1f;

            var minX = math.max(0, (int)math.floor(center.x - radiusPx));
            var maxX = math.min(resolution - 1, (int)math.ceil(center.x + radiusPx));
            var minY = math.max(0, (int)math.floor(center.y - radiusPx));
            var maxY = math.min(resolution - 1, (int)math.ceil(center.y + radiusPx));

            var tintR = splat.Tint.r * 255f;
            var tintG = splat.Tint.g * 255f;
            var tintB = splat.Tint.b * 255f;
            var maxStrength = splat.Tint.a;

            for (var py = minY; py <= maxY; py++)
            {
                for (var px = minX; px <= maxX; px++)
                {
                    float dx = px - center.x;
                    float dy = py - center.y;
                    var dist = math.sqrt(dx * dx + dy * dy);
                    if (dist > radiusPx)
                        continue;

                    var strength = (1f - dist / radiusPx) * maxStrength;
                    if (strength <= 0f)
                        continue;

                    var index = py * resolution + px;
                    var current = pixels[index];
                    pixels[index] = new Color32(
                        (byte)Mathf.Lerp(current.r, tintR, strength),
                        (byte)Mathf.Lerp(current.g, tintG, strength),
                        (byte)Mathf.Lerp(current.b, tintB, strength),
                        current.a);
                }
            }
        }

        /// <summary>
        ///     Converts a world XZ position to integer pixel coordinates using the square UV rect.
        ///     Mirrors <see cref="TerrainViewTextureSubSystem" />.WorldToPixel.
        /// </summary>
        private static Vector2Int WorldToPixel(float2 worldXZ, int resolution, in UvRect uv)
        {
            var u = (worldXZ.x - uv.Min.x) / uv.Size;
            var v = (worldXZ.y - uv.Min.y) / uv.Size;
            return new Vector2Int(
                (int)(u * (resolution - 1)),
                (int)(v * (resolution - 1)));
        }
    }
}
