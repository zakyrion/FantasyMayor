using Cysharp.Threading.Tasks;
using Modules.Hexes.DataLayer;
using UnityEngine;

/// <summary>
/// Generate hexes and vector fields
/// </summary>
public class TerrainHexViewGenerator : ISurfaceGenerator
{
    private TerrainGeneratorSettingsScriptable _terrainSettings;
    private HexesViewDataLayer _hexesDataLayer;
    private IHexesAPI _hexesAPI;
    private int _waves;
    
    public TerrainHexViewGenerator(TerrainGeneratorSettingsScriptable terrainSettings, HexesViewDataLayer hexesDataLayer, IHexesAPI hexesAPI)
    {
        _terrainSettings = terrainSettings;
        _hexesDataLayer = hexesDataLayer;
        _hexesAPI = hexesAPI;
    }
    
    public async UniTask<bool> Generate(int waves)
    {
        _waves = waves;
        Debug.Log("[skh] TerrainGeneratorService.CreateTerrain()");

        var spawnHexesByWavesCommand = new SpawnHexesByWavesCommand(_hexesDataLayer);
        var spawnPointsCommand = new SpawnPointsCommand(_hexesDataLayer);
        var spawnHexesCommand = new SpawnHexesCommand(_hexesDataLayer, _hexesAPI);

        spawnHexesByWavesCommand.Execute(_waves, _terrainSettings.HexSize);

        spawnPointsCommand.Execute(_terrainSettings.HexSize);
        await spawnHexesCommand.Execute(_terrainSettings.HexDetailLevel);

        HexVectorUtil.TriangleSegmentSize = _terrainSettings.TriangleSize;

        ConvertToVectorField();

        return true;
    }
    
    private void ConvertToVectorField()
    {
        /*ref var hexVectors = ref _hexDataLayer.HexVectors;

        foreach (var hex in _hexDataLayer.Hexes)
        {
            var vertices = hex.Vertices;
            foreach (var vertex in vertices)
            {
                var gridPosition = HexVectorUtil.CalculateGridPosition(vertex);
                var hexVector = new FieldsVector(gridPosition, vertex);

                if (!hexVectors.ContainsKey(hexVector.GridPosition))
                    hexVectors.Add(hexVector.GridPosition, hexVector);
            }
        }*/
    }
}