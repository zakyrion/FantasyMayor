using UnityEngine;

namespace Modules.CurveBuilders
{
    public interface ICurveBuilder
    {

        AnimationCurve BuildPreviewCurve(float lengthMultiplier, int samplesPerUnit);
        float GetValue(float t);
    }
}
