using UnityEngine;
using VContainer;
using VContainer.Unity;

public class SampleSceneScope : LifetimeScope
{
    [SerializeField] private DataLayerInstaller _dataLayerInstaller;
    [SerializeField] private HexesInstallers _hexesInstallers;
    [SerializeField] private TerrainGenerationInstaller _terrainGenerationInstaller;


    protected override void Configure(IContainerBuilder builder)
    {
        _dataLayerInstaller.InstallBindings(builder);
        _hexesInstallers.InstallBindings(builder);
        _terrainGenerationInstaller.InstallBindings(builder);

        builder.Register<PathfindingSystem>(Lifetime.Scoped).AsSelf().AsImplementedInterfaces();
    }
}