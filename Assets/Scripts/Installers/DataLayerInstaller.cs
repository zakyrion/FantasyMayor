using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "DataLayerInstaller", menuName = "Installers/DataLayerInstaller")]
public class DataLayerInstaller : ScriptableObjectInstaller<DataLayerInstaller>
{
    [SerializeField] private TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;

    public override void InstallBindings()
    {
    }
}
