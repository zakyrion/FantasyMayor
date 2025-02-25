using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "RegionShapeShiftSettingsScriptable",
    menuName = "Settings/RegionShapeShiftSettingsScriptable")]
public class RegionShapeShiftSettingsScriptable : ScriptableObject
{
    public List<ShapeShiftSetting> Settings = new();

    [ContextMenu("Generate")]
    public void Generate()
    {
        for (var i = 0; i < 20; i++)
        {
            var curve = new AnimationCurve();

            var pointsInCurve = Random.Range(10, 64);
            curve.AddKey(0, Random.value);

            for (var j = 1; j < pointsInCurve; j++)
            {
                do
                {
                    var oldValue = curve.keys[^1].value;
                    var newKey = math.clamp((Random.value * 2 - 1) * math.sin((Random.value - .5f) * 1000), -1, 1);

                    if (math.abs(oldValue - newKey) > .2f)
                    {
                        curve.AddKey(j * Random.value + .5f, newKey);
                        break;
                    }

                } while (true);
            }

            curve.AddKey(curve.keys[curve.length - 1].time + .5f, curve.keys[0].value);
            var setting = new ShapeShiftSetting {Weight = 1, ShiftCurve = curve};
            Settings.Add(setting);
        }
    }

    public AnimationCurve GetRandomSetting()
    {
        var randomWeight = Random.Range(0, Settings.Sum(s => s.Weight));
        var cumulativeWeight = 0;
        foreach (var setting in Settings)
        {
            cumulativeWeight += setting.Weight;
            if (randomWeight < cumulativeWeight) return setting.ShiftCurve;
        }

        return default;
    }
}

[Serializable]
public struct ShapeShiftSetting
{
    public AnimationCurve ShiftCurve;
    public int Weight;
}