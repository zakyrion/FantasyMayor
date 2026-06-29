using Modules.CurveBuilders;
using Unity.Mathematics;
using UnityEngine;

namespace Presentation.Terrain.CurveBuilders
{
    public sealed class SineCurveBuilder : ICurveBuilder
    {
        private readonly float _frequency;

        public SineCurveBuilder(float frequency)
        {
            _frequency = math.max(0.0001f, frequency);
        }

        public AnimationCurve BuildPreviewCurve(float lengthMultiplier, int samplesPerUnit)
        {
            var safeLength = math.max(1f, lengthMultiplier);
            var safeSamplesPerUnit = math.max(2, samplesPerUnit);
            var keyCount = math.max(2, (int)math.ceil(safeLength * safeSamplesPerUnit) + 1);

            var keys = new Keyframe[keyCount];
            for (var i = 0; i < keyCount; i++)
            {
                var time = safeLength * i / (keyCount - 1f);
                var normalized = i == keyCount - 1 ? 1f : math.frac(time);
                keys[i] = new Keyframe(time, GetValue(normalized));
            }

            return new AnimationCurve(keys);
        }

        public float GetValue(float t)
        {
            var clamped = math.saturate(t);
            var wave = 0.5f * (math.sin(2f * math.PI * clamped * _frequency) + 1f);
            return math.saturate(wave);
        }
    }
}
