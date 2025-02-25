using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Generate hexes and vector fields
/// </summary>
public class TerrainHexViewGenerator : ISurfaceGenerator
{
    private TerrainGeneratorSettingsScriptable _terrainSettings;
    private HexViewDataLayer _hexDataLayer;
    private IHexesAPI _hexesAPI;
    private int _waves;
    
    public TerrainHexViewGenerator(TerrainGeneratorSettingsScriptable terrainSettings, HexViewDataLayer hexDataLayer, IHexesAPI hexesAPI)
    {
        _terrainSettings = terrainSettings;
        _hexDataLayer = hexDataLayer;
        _hexesAPI = hexesAPI;
    }
    
    public async UniTask<bool> Generate(int waves)
    {
        _waves = waves;
        Debug.Log("[skh] TerrainGeneratorService.CreateTerrain()");

        var spawnHexesByWavesCommand = new SpawnHexesByWavesCommand(_hexDataLayer);
        var spawnPointsCommand = new SpawnPointsCommand(_hexDataLayer);
        var spawnHexesCommand = new SpawnHexesCommand(_hexDataLayer, _hexesAPI);

        spawnHexesByWavesCommand.Execute(_waves, _terrainSettings.HexSize);

        spawnPointsCommand.Execute(_terrainSettings.HexSize);
        await spawnHexesCommand.Execute(_terrainSettings.HexDetailLevel);

        HexVectorUtil.TriangleSegmentSize = _terrainSettings.TriangleSize;

        ConvertToVectorField();

        return true;
    }
    
    private void ConvertToVectorField()
    {
        ref var hexVectors = ref _hexDataLayer.HexVectors;

        foreach (var hex in _hexDataLayer.Hexes)
        {
            var vertices = hex.Vertices;
            foreach (var vertex in vertices)
            {
                var gridPosition = HexVectorUtil.CalculateGridPosition(vertex);
                var hexVector = new FieldsVector(gridPosition, vertex, hex.Position);

                if (!hexVectors.ContainsKey(hexVector.GridPosition))
                    hexVectors.Add(hexVector.GridPosition, hexVector);
            }
        }
    }
}