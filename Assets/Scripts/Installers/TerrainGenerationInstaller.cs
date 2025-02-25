using Sirenix.OdinInspector;
using UnityEngine;
using Zenject;

public class TerrainGenerationInstaller : MonoInstaller
{
    [SerializeField] private StartService _startService;
    [SerializeField] private TerrainGeneratorService _terrainGenerator;
    [SerializeField] private SpotGenerator _spotGenerator;
    [SerializeField] private HeightMapsGenerator _heightMapsGenerator;
    
    [Title("Scriptables")]
    [SerializeField] private HeightsGeneratorSettingsScriptable _heightsGeneratorSettings;
    [SerializeField] private RegionShapeShiftSettingsScriptable _regionShapeShiftSettings;

    public override void InstallBindings()
    {
        var pagesDataLayer = new GeneratorPagesDataLayer();

        Container.BindInterfacesAndSelfTo<GeneratorPagesDataLayer>().FromInstance(pagesDataLayer);
        Container.Bind<StartService>().FromInstance(_startService);
        Container.BindInterfacesAndSelfTo<TerrainGeneratorService>().FromInstance(_terrainGenerator);
        Container.Bind<SpotGenerator>().FromInstance(_spotGenerator);
        Container.Bind<HeightMapsGenerator>().FromInstance(_heightMapsGenerator);
        Container.Bind<HeightsGeneratorSettingsScriptable>().FromInstance(_heightsGeneratorSettings);
        Container.Bind<RegionShapeShiftSettingsScriptable>().FromInstance(_regionShapeShiftSettings);

        Container.Bind<MountsGenerator>().AsSingle();
        Container.Bind<WaterGeneratorService>().AsSingle();
        Container.Bind<TerrainHexViewGenerator>().AsSingle();
        Container.Bind<TerrainLevelGenerator>().AsSingle();

        Container.Bind<HeightmapDataLayer>().AsSingle();

        Debug.Log("[skh] TerrainGenerationInstaller.InstallBindings()");
    }
}