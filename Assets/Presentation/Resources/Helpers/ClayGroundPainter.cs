using Modules.AxialSystem;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Presentation.Resources.Helpers
{
    /// <summary>
    ///     Paints clay patches into the persistent terrain <see cref="Texture2D" />. Each patch is a
    ///     radial gradient from a wet, dark center color to a dry, light rim color, masked by a shared
    ///     <see cref="ClayFootprint" /> so the painted clay matches the geometry depression exactly.
    ///     One-shot by design: clay is a persistent resource that never disappears, so there is no
    ///     baseline/repaint bookkeeping (unlike <see cref="ForestGroundPainter" />). The world-to-pixel
    ///     mapping duplicates <c>TerrainViewTextureSubSystem</c> so patches land in the same UV space as
    ///     the baked texture — the same deliberate DoD duplication used by the forest painter.
    /// </summary>
    internal sealed class ClayGroundPainter
    {
        // Fraction of the radius over which coverage feathers to zero at the edge, blending into terrain.
        private const float RimFeather = 0.25f;

        private Texture2D _texture;
        private int _resolution;
        private Color32[] _working;
        private float2 _squareMin;
        private float _squareSize;
        private bool _initialized;

        public bool IsInitialized => _initialized;

        /// <summary>
        ///     Captures the current terrain pixels as the working buffer and computes the square UV rect
        ///     from all hex coords. Clay is painted directly on top — it becomes part of the terrain that
        ///     later systems (e.g. the forest painter baseline) read back.
        /// </summary>
        /// <param name="hexCoords">All hex coordinates, used to reproduce the bake-time square UV rect.</param>
        /// <param name="texture">Persistent terrain texture, created readable (Apply(false)).</param>
        /// <param name="hexSize">Hex cell radius in world units (pointy-top), i.e. terrain CellSize.</param>
        public void Initialize(NativeArray<HexCoord> hexCoords, Texture2D texture, float hexSize)
        {
            _texture = texture;
            _resolution = texture.width;
            _working = texture.GetPixels32();
            ComputeSquareUVRect(hexCoords, hexSize, out _squareMin, out _squareSize);
            _initialized = true;
        }

        /// <summary>Blends one clay patch into the working buffer using the shared footprint as its mask.</summary>
        public void PaintClay(in ClayFootprint footprint, float2 centerXZ, Color centerColor, Color rimColor)
        {
            if (!_initialized || _squareSize <= 0f)
                return;

            var center = WorldToPixel(centerXZ);
            var radiusPx = math.max(1f, footprint.MaxWorldRadius / _squareSize * (_resolution - 1));

            var minX = math.max(0, (int)math.floor(center.x - radiusPx));
            var maxX = math.min(_resolution - 1, (int)math.ceil(center.x + radiusPx));
            var minY = math.max(0, (int)math.floor(center.y - radiusPx));
            var maxY = math.min(_resolution - 1, (int)math.ceil(center.y + radiusPx));

            for (var py = minY; py <= maxY; py++)
            {
                for (var px = minX; px <= maxX; px++)
                {
                    var worldXZ = PixelToWorld(px, py);
                    if (!footprint.Evaluate(centerXZ, worldXZ, out var t))
                        continue;

                    var coverage = t < 1f - RimFeather ? 1f : 1f - (t - (1f - RimFeather)) / RimFeather;
                    if (coverage <= 0f)
                        continue;

                    var color = Color.Lerp(centerColor, rimColor, t);
                    var index = py * _resolution + px;
                    var current = _working[index];
                    _working[index] = new Color32(
                        (byte)Mathf.Lerp(current.r, color.r * 255f, coverage),
                        (byte)Mathf.Lerp(current.g, color.g * 255f, coverage),
                        (byte)Mathf.Lerp(current.b, color.b * 255f, coverage),
                        current.a);
                }
            }
        }

        /// <summary>Uploads the painted buffer to the texture once. Call after all patches are painted.</summary>
        public void Apply()
        {
            if (!_initialized)
                return;

            _texture.SetPixels32(_working);
            _texture.Apply(false);
        }

        private Vector2Int WorldToPixel(float2 worldXZ)
        {
            var u = (worldXZ.x - _squareMin.x) / _squareSize;
            var v = (worldXZ.y - _squareMin.y) / _squareSize;
            return new Vector2Int(
                (int)(u * (_resolution - 1)),
                (int)(v * (_resolution - 1)));
        }

        private float2 PixelToWorld(int px, int py)
        {
            var u = (float)px / (_resolution - 1);
            var v = (float)py / (_resolution - 1);
            return new float2(
                _squareMin.x + u * _squareSize,
                _squareMin.y + v * _squareSize);
        }

        /// <summary>
        ///     Computes a square UV rect from hex centers plus their six pointy-top corners.
        ///     Mirrors <c>TerrainViewTextureSubSystem</c>.ComputeSquareUVRect so vertex positions map to
        ///     the identical UV space as the baked texture.
        /// </summary>
        private void ComputeSquareUVRect(
            NativeArray<HexCoord> hexCoords,
            float hexSize,
            out float2 squareMin,
            out float squareSize)
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

            var sizeX = maxX - minX;
            var sizeZ = maxZ - minZ;
            squareSize = math.max(sizeX, sizeZ);

            var centerX = (minX + maxX) * 0.5f;
            var centerZ = (minZ + maxZ) * 0.5f;
            squareMin = new float2(centerX - squareSize * 0.5f, centerZ - squareSize * 0.5f);
        }
    }
}
