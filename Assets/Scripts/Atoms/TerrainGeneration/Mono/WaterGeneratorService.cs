using System;
using HW.Data;
using UniRx;
using Unity.Mathematics;
using UnityEngine;
using VContainer;
using static WaterGeneratorDataLayer;
using Random = Unity.Mathematics.Random;

public class WaterGeneratorService : IDisposable
{
    private readonly DisposeHandler _disposables = new();
    private readonly HeightMapsGenerator _heightMapsGenerator;
    private readonly HeightsGeneratorSettingsScriptable _heightsGeneratorSettings;
    private readonly HexViewDataLayer _hexDataLayer;

    private readonly SeedDataLayer _seedDataLayer;
    private readonly SpotGenerator _spotGenerator;
    private readonly TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;
    private readonly WaterGeneratorDataLayer _waterGeneratorDataLayer;
    private IPathfindingAPI _pathfindingAPI;

    private Random _random;


    [Inject]
    public WaterGeneratorService(SeedDataLayer seedDataLayer, HexViewDataLayer hexDataLayer,
        TerrainGeneratorSettingsScriptable terrainGeneratorSettingsScriptable, HeightMapsGenerator heightMapsGenerator,
        HeightsGeneratorSettingsScriptable heightsGeneratorSettings, SpotGenerator spotGenerator,
        WaterGeneratorDataLayer waterGeneratorDataLayer, IPathfindingAPI pathfindingAPI)
    {
        _seedDataLayer = seedDataLayer;
        _hexDataLayer = hexDataLayer;
        _terrainGeneratorSettingsScriptable = terrainGeneratorSettingsScriptable;
        _heightMapsGenerator = heightMapsGenerator;
        _heightsGeneratorSettings = heightsGeneratorSettings;
        _spotGenerator = spotGenerator;
        _waterGeneratorDataLayer = waterGeneratorDataLayer;
        _pathfindingAPI = pathfindingAPI;

        _random = new Random(seedDataLayer.Seed.Value);

        _disposables.AddDisposable(_seedDataLayer.Seed.Subscribe(_ => _random = new Random(seedDataLayer.Seed.Value)));
    }

    public void Dispose()
    {
        _disposables.DisposeAll();
    }

    public void Generate(int waves)
    {
        switch (_waterGeneratorDataLayer.Type)
        {
            case WaterType.Lake:
                //GenerateLake();
                break;
            case WaterType.River:
                GenerateRiver(waves);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void GenerateRiver(int waves)
    {
        var positions = MapSpawnSettings.GetRandomHexPairPositions(waves);

        Debug.DrawLine(_hexDataLayer[positions.Item1].Position3D,
            _hexDataLayer[positions.Item1].Position3D + new float3(0, 10, 0), Color.blue, 100);

        Debug.DrawLine(_hexDataLayer[positions.Item2].Position3D,
            _hexDataLayer[positions.Item2].Position3D + new float3(0, 10, 0), Color.red, 100);

        var path = _pathfindingAPI.GetPath(positions.Item1, positions.Item2);
        
        foreach (var hex in path)
        {
            _hexDataLayer[hex].SurfaceType = SurfaceType.Water;
            Debug.DrawLine(_hexDataLayer[hex].Position3D,
                _hexDataLayer[hex].Position3D + new float3(0, 10, 0), Color.green, 100);
        }

        _spotGenerator.GenerateRiverSpot(path);

    }
}