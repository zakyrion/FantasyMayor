using Modules.AxialSystem;
using Unity.Mathematics;

namespace Presentation.HexResources.Helpers
{
    /// <summary>
    ///     Single source of truth for one clay patch contour. Both the geometry depression
    ///     (<see cref="ClayDepressionShaper" />) and the texture paint (<see cref="ClayGroundPainter" />)
    ///     query this so the sunk mesh and the painted clay share the exact same outline.
    ///     The base shape is an ellipse (<c>aspect</c>) optionally pulled into a
    ///     pear (<c>pearFactor</c>); a deterministic <c>snoise</c> ring then perturbs the radius
    ///     per direction for an organic, non-circular edge. The noise is seeded from the hex coords, so
    ///     every clay hex looks different yet stays stable across frames and runs.
    /// </summary>
    internal readonly struct ClayFootprint
    {
        // Neighbouring hexes differ by 1 in axial space; sampling 100 units apart decorrelates them.
        private const float SeedSpread = 100f;
        // Keeps the radius strictly positive even at NoiseAmplitude = 1 / PearFactor extremes.
        private const float MinRadiusFactor = 0.15f;

        private readonly float _baseRadius;
        private readonly float2 _semiAxis; // ellipse semi-axes in world units (a along X, b along Z)
        private readonly float _pearFactor;
        private readonly float _noiseAmplitude;
        private readonly float _noiseFrequency;
        private readonly float2 _seed;

        /// <summary>
        ///     Conservative upper bound on the contour radius in world units. The painter uses it to size
        ///     the pixel bounding box; pixels outside the actual contour are still rejected by
        ///     <see cref="Evaluate" />.
        /// </summary>
        public float MaxWorldRadius { get; }

        public ClayFootprint(
            HexCoord hex,
            float baseRadius,
            float2 aspect,
            float pearFactor,
            float noiseAmplitude,
            float noiseFrequency)
        {
            _baseRadius = baseRadius;
            _semiAxis = new float2(baseRadius * math.max(aspect.x, 0.01f), baseRadius * math.max(aspect.y, 0.01f));
            _pearFactor = pearFactor;
            _noiseAmplitude = noiseAmplitude;
            _noiseFrequency = noiseFrequency;
            _seed = new float2(hex.Value.x, hex.Value.y) * SeedSpread;

            var maxAxis = math.max(_semiAxis.x, _semiAxis.y);
            MaxWorldRadius = maxAxis * (1f + math.max(0f, pearFactor)) * (1f + math.max(0f, noiseAmplitude));
        }

        /// <summary>
        ///     Returns whether <paramref name="pointXZ" /> lies inside the contour around
        ///     <paramref name="centerXZ" /> and, if so, the normalized radial position
        ///     <paramref name="t" /> (0 at the center, 1 at the edge) used for falloff and the color gradient.
        /// </summary>
        public bool Evaluate(float2 centerXZ, float2 pointXZ, out float t)
        {
            var dir = pointXZ - centerXZ;
            var dist = math.length(dir);
            if (dist <= math.EPSILON)
            {
                t = 0f;
                return _baseRadius > 0f;
            }

            var unit = dir / dist;
            var radius = DirectionalRadius(unit);
            t = dist / radius;
            return t < 1f;
        }

        /// <summary>Contour radius along a unit direction: ellipse → pear → noise ring.</summary>
        private float DirectionalRadius(float2 unit)
        {
            // Ellipse radius at this angle: r = (a*b) / sqrt((b*cos)^2 + (a*sin)^2).
            var a = _semiAxis.x;
            var b = _semiAxis.y;
            var bc = b * unit.x;
            var as_ = a * unit.y;
            var ellipse = a * b / math.sqrt(bc * bc + as_ * as_);

            // Pear: bulge toward +Z, narrow toward -Z (kept positive).
            var pear = math.max(MinRadiusFactor, 1f + _pearFactor * unit.y);

            // Organic edge: sample snoise on a closed ring so the contour stays continuous around the loop.
            var n = noise.snoise(_seed + unit * _noiseFrequency); // [-1, 1]
            var noiseFactor = math.max(MinRadiusFactor, 1f + _noiseAmplitude * n);

            return ellipse * pear * noiseFactor;
        }
    }
}
