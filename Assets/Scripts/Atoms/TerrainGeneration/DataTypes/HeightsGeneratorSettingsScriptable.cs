using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeightsGeneratorSettings", menuName = "Settings/HeightsGeneratorSettings")]
public class HeightsGeneratorSettingsScriptable : ScriptableObject
{
    [SerializeField] private List<HeightsGeneratorSettings> _settings = new();

    public List<HeightsGeneratorSettings> GetSettings(SurfaceType terrainType)
    {
        return _settings.FindAll(settings => settings._surfaceType == terrainType);
    }

    public HeightsGeneratorSettings GetRandomSettings(SurfaceType terrainType)
    {
        var settings = GetSettings(terrainType);
        return settings[Random.Range(0, settings.Count)];
    }

    public HeightsGeneratorSettings GetSettings(SurfaceType terrainType, int index)
    {
        return _settings.Find(settings => settings._surfaceType == terrainType && _settings.IndexOf(settings) == index);
    }
}