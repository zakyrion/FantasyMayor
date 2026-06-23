using Modules.CurveBuilders;
using Unity.Mathematics;
using UnityEngine;

namespace Presentation.Terrain.CurveBuilders
{
    public sealed class NaturalLineCurveBuilder : ICurveBuilder
    {
        private const int END_KEY_OFFSET = 1;
        private const float END_KEY_TIME_OFFSET = 1f;

        private const float FULL_CIRCLE_MULTIPLIER = 2f;
        private const float HALF = 0.5f;
        private const float INITIAL_AMPLITUDE = 1f;
        private const float INITIAL_AMPLITUDE_SUM = 0f;
        private const float INITIAL_NOISE_SUM = 0f;

        private const float INITIAL_SMOOTHING_STEP = 1f / 128f;
        private const float MAX_NORMALIZED_VALUE = 1f;
        private const float MAX_PERSISTENCE = 1f;
        private const float MIN_BASE_FREQUENCY = 0.0001f;
        private const int MIN_KEY_COUNT = 2;
        private const float MIN_LACUNARITY = 1f;
        private const float MIN_NORMALIZED_VALUE = 0f;
        private const int MIN_OCTAVES = 1;
        private const float MIN_PERSISTENCE = 0.01f;

        private const float MIN_PREVIEW_LENGTH = 1f;
        private const int MIN_SAMPLES_PER_UNIT = 2;
        private const int MIN_SMOOTHING_PASSES = ZERO_INT;
        private const float SEED_X_MULTIPLIER = 0.173f;
        private const float SEED_Y_MULTIPLIER = 0.247f;
        private const float SMOOTHING_AVERAGE_DIVISOR = 3f;
        private const float SMOOTHING_STEP_GROWTH = 1.35f;
        private const int ZERO_INT = 0;

        private readonly float _baseFrequency;
        private readonly float _lacunarity;
        private readonly int _octaves;
        private readonly float _persistence;
        private readonly int _seed;
        private readonly int _smoothingPasses;

        public NaturalLineCurveBuilder(
            float baseFrequency,
            int octaves,
            float persistence,
            float lacunarity,
            int smoothingPasses,
            int seed)
        {
            _baseFrequency = math.max(MIN_BASE_FREQUENCY, baseFrequency);
            _octaves = math.max(MIN_OCTAVES, octaves);
            _persistence = math.clamp(persistence, MIN_PERSISTENCE, MAX_PERSISTENCE);
            _lacunarity = math.max(MIN_LACUNARITY, lacunarity);
            _smoothingPasses = math.max(MIN_SMOOTHING_PASSES, smoothingPasses);
            _seed = seed;
        }

        public AnimationCurve BuildPreviewCurve(float lengthMultiplier, int samplesPerUnit)
        {
            var safeLength = math.max(MIN_PREVIEW_LENGTH, lengthMultiplier);
            var safeSamplesPerUnit = math.max(MIN_SAMPLES_PER_UNIT, samplesPerUnit);
            var keyCount = math.max(MIN_KEY_COUNT, (int)math.ceil(safeLength * safeSamplesPerUnit) + END_KEY_OFFSET);

            var keys = new Keyframe[keyCount];
            for (var i = ZERO_INT; i < keyCount; i++)
            {
                var time = safeLength * i / (keyCount - END_KEY_TIME_OFFSET);
                var normalized = i == keyCount - END_KEY_OFFSET ? MAX_NORMALIZED_VALUE : math.frac(time);
                keys[i] = new Keyframe(time, GetValue(normalized));
            }

            return new AnimationCurve(keys);
        }

        public float GetValue(float t)
        {
            var wrapped = math.frac(math.max(MIN_NORMALIZED_VALUE, t));
            var value = SampleRawNoise01(wrapped);

            if (_smoothingPasses == MIN_SMOOTHING_PASSES)
                return value;

            var step = INITIAL_SMOOTHING_STEP;
            for (var pass = ZERO_INT; pass < _smoothingPasses; pass++)
            {
                var prev = SampleRawNoise01(wrapped - step);
                var next = SampleRawNoise01(wrapped + step);
                value = (prev + value + next) / SMOOTHING_AVERAGE_DIVISOR;
                step *= SMOOTHING_STEP_GROWTH;
            }

            return math.clamp(value, MIN_NORMALIZED_VALUE, MAX_NORMALIZED_VALUE);
        }

        private float SampleRawNoise01(float t)
        {
            var wrapped = math.frac(math.max(MIN_NORMALIZED_VALUE, t));
            var angle = FULL_CIRCLE_MULTIPLIER * math.PI * wrapped;
            var circle = new float2(math.cos(angle), math.sin(angle));
            var seedOffset = new float2(_seed * SEED_X_MULTIPLIER, _seed * SEED_Y_MULTIPLIER);

            var amplitude = INITIAL_AMPLITUDE;
            var amplitudeSum = INITIAL_AMPLITUDE_SUM;
            var noiseSum = INITIAL_NOISE_SUM;
            var frequency = _baseFrequency;

            for (var octave = ZERO_INT; octave < _octaves; octave++)
            {
                var p = circle * frequency + seedOffset;
                noiseSum += noise.snoise(p) * amplitude;
                amplitudeSum += amplitude;
                amplitude *= _persistence;
                frequency *= _lacunarity;
            }

            if (amplitudeSum <= INITIAL_AMPLITUDE_SUM)
                return HALF;

            return math.clamp(HALF * (noiseSum / amplitudeSum + MAX_NORMALIZED_VALUE), MIN_NORMALIZED_VALUE, MAX_NORMALIZED_VALUE);
        }
    }
}
