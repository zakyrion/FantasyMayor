using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

public class TerrainGenerationInstaller : MonoBehaviour
{
    [SerializeField] private StartService _startService;
    [SerializeField] private TerrainGeneratorService _terrainGenerator;
    [SerializeField] private SpotGenerator _spotGenerator;
    [SerializeField] private HeightMapsGenerator _heightMapsGenerator;

    [Title("Scriptables")] [SerializeField]
    private HeightsGeneratorSettingsScriptable _heightsGeneratorSettings;

    public void InstallBindings(IContainerBuilder builder)
    {
        builder.RegisterInstance(_startService);
        builder.RegisterInstance(_terrainGenerator).AsImplementedInterfaces().AsSelf();
        builder.RegisterInstance(_spotGenerator).AsImplementedInterfaces().AsSelf();
        builder.RegisterInstance(_heightMapsGenerator).AsImplementedInterfaces().AsSelf();
        builder.RegisterInstance(_heightsGeneratorSettings).AsImplementedInterfaces().AsSelf();

        builder.Register<MountsGenerator>(Lifetime.Scoped);
        builder.Register<WaterGeneratorService>(Lifetime.Scoped);
        builder.Register<TerrainHexViewGenerator>(Lifetime.Scoped);
        builder.Register<TerrainLevelGenerator>(Lifetime.Scoped);
        builder.Register<HeightmapDataLayer>(Lifetime.Scoped);
    }
}