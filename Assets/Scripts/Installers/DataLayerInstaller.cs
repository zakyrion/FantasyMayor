using UnityEngine;
using VContainer;

[CreateAssetMenu(fileName = "DataLayerInstaller", menuName = "Installers/DataLayerInstaller")]
public class DataLayerInstaller : ScriptableObject
{
    [SerializeField] private TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;

    public void InstallBindings(IContainerBuilder builder)
    {
        builder.Register<DataLayer>(Lifetime.Scoped);

        builder.Register<HexViewDataLayer>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        builder.Register<WaterGeneratorDataLayer>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        builder.Register<MountsGeneratorDataLayer>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        builder.Register<SeedDataLayer>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        builder.RegisterInstance(_terrainGeneratorSettingsScriptable).AsImplementedInterfaces().AsSelf();
    }
}