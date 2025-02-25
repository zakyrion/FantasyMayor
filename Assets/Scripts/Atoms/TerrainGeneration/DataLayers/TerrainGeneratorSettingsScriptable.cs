using UnityEngine;

[CreateAssetMenu(fileName = "TerrainGeneratorSettingsScriptable",
    menuName = "Settings/TerrainGeneratorSettingsScriptable")]
public class TerrainGeneratorSettingsScriptable : ScriptableObject, IDataContainer
{
    [field: SerializeField] public int DecorationMapResolution { get; private set; }

    [field: SerializeField] public int GlobalMapResolution { get; private set; }

    [field: SerializeField] public float HexSize { get; private set; } = 1f;
    [field: SerializeField] public float HeightPerLevel { get; private set; } = 1;

    [field: SerializeField] public int HexDetailLevel { get; private set; } = 20;
    public float TriangleSize => HexSize / HexDetailLevel;

    public void Dispose()
    {
        // TODO release managed resources here
    }
}