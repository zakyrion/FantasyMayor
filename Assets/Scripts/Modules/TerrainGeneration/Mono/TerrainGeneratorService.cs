using System.Collections.Generic;
using Modules.Hexes.DataLayer;
using Modules.Hexes.DataTypes;
using UnityEngine;
using Zenject;

public class TerrainGeneratorService : MonoBehaviour, ITerrainGenerationAPI
{
    private readonly TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;
    private HexViewDataLayer _hexDataLayer;
    private MountsGenerator _mountsGeneratorService;
    private SpotGenerator _spotGenerator;
    private TerrainHexViewGenerator _surfaceGeneratorService;
    private TerrainLevelGenerator _terrainLevelGenerator;

    private int _waves;

    public void Apply()
    {
        //TODO path from highest to lovest levels and build heightmaps for all clasters
        var levels = new HashSet<int>();
        foreach (var hexData in _hexDataLayer.Hexes) levels.Add(hexData.Level.Value);

        levels.Remove(0);

        foreach (var level in levels)
        {
            var hexesAtLayer = new HashSet<HexId>();

            foreach (var hexData in _hexDataLayer.Hexes)
            {
                if (hexData.Level.Value == level)
                    hexesAtLayer.Add(hexData.HexId);
            }

            while (hexesAtLayer.Count > 0)
            {
                var toCheck = new List<HexId>();

                using var enumerator = hexesAtLayer.GetEnumerator();
                enumerator.MoveNext();
                toCheck.Add(enumerator.Current);

                var spot = new List<HexId>();

                while (toCheck.Count > 0)
                {
                    var nextToCheck = new List<HexId>();

                    for (var i = 0; i < toCheck.Count; i++)
                    {
                        var hex = _hexDataLayer[toCheck[i]];
                        spot.Add(hex.HexId);
                        hexesAtLayer.Remove(hex.HexId);

                        foreach (var neighbor in hex.Neighbors)
                        {
                            if (neighbor.Level.Value == level && hexesAtLayer.Contains(neighbor.HexId))
                            {
                                nextToCheck.Add(neighbor.HexId);
                                hexesAtLayer.Remove(neighbor.HexId);
                            }
                        }
                    }

                    toCheck.Clear();
                    toCheck.AddRange(nextToCheck);
                }

                //_mountsGeneratorService.AddHill(spot);

                _terrainLevelGenerator.Generate(spot, level);
            }
        }
    }

    async void ITerrainGenerationAPI.GenerateMount()
    {
        await _mountsGeneratorService.Generate(null);
    }

    async void ITerrainGenerationAPI.CreateTerrainVectorField(int waves)
    {
        _waves = waves;

        await _surfaceGeneratorService.Generate(_waves);
        //StartCoroutine(DrawBorders());
    }

    [Inject]
    private void Init(MountsGenerator mountsGeneratorService,
        TerrainHexViewGenerator surfaceGeneratorService,
        SpotGenerator spotGenerator, HexViewDataLayer hexDataLayer, TerrainLevelGenerator terrainLevelGenerator)
    {
        Debug.Log("[skh] TerrainGeneratorService.Init()");

        _mountsGeneratorService = mountsGeneratorService;
        _surfaceGeneratorService = surfaceGeneratorService;
        _terrainLevelGenerator = terrainLevelGenerator;

        _spotGenerator = spotGenerator;
        _hexDataLayer = hexDataLayer;
    }
}