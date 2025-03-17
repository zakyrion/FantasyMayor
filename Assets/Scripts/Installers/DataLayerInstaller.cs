using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "DataLayerInstaller", menuName = "Installers/DataLayerInstaller")]
public class DataLayerInstaller : ScriptableObjectInstaller<DataLayerInstaller>
{
    [SerializeField] private TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;

    public override void InstallBindings()
    {
        Container.Bind<DataLayer>().AsSingle();

        InitDataLayers();
    }

    private void InitDataLayers()
    {
        Container.BindInterfacesAndSelfTo<HexViewDataLayer>().AsSingle();
        Container.BindInterfacesAndSelfTo<SeedDataLayer>().AsSingle();
        Container.BindInterfacesAndSelfTo<TerrainGeneratorSettingsScriptable>()
            .FromInstance(_terrainGeneratorSettingsScriptable).AsSingle();
        Container.BindInterfacesAndSelfTo<WaterGeneratorDataLayer>().AsSingle();
        Container.BindInterfacesAndSelfTo<MountsGeneratorDataLayer>().AsSingle();
    }
}