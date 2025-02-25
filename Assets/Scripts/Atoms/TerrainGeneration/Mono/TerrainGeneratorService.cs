using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

public class TerrainGeneratorService : MonoBehaviour, ITerrainGenerationAPI
{
    private HexViewDataLayer _hexDataLayer;
    private MountsGenerator _mountsGeneratorService;
    private SpotGenerator _spotGenerator;
    private TerrainHexViewGenerator _surfaceGeneratorService;
    private TerrainLevelGenerator _terrainLevelGenerator;
    private WaterGeneratorService _waterGeneratorService;
    
    private readonly TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;

    private int _waves;

    void ITerrainGenerationAPI.GenerateWater()
    {
        _waterGeneratorService.Generate(_waves);
    }

    public void Apply()
    {
        //TODO path from highest to lovest levels and build heightmaps for all clasters
        var levels = new HashSet<int>();
        foreach (var hexData in _hexDataLayer.Hexes) levels.Add(hexData.Level.Value);

        levels.Remove(0);
        
        foreach (var level in levels)
        {
            var hexesAtLayer = new NativeHashSet<int2>(_hexDataLayer.Hexes.Count, Allocator.Temp);

            foreach (var hexData in _hexDataLayer.Hexes)
            {
                if (hexData.Level.Value == level)
                    hexesAtLayer.Add(hexData.Position);
            }

            while (hexesAtLayer.Count > 0)
            {
                var toCheck = new NativeList<int2>(Allocator.Temp);
                
                using var enumerator = hexesAtLayer.GetEnumerator();
                enumerator.MoveNext();
                toCheck.Add(enumerator.Current);

                var spot = new NativeList<int2>(Allocator.Temp);
                //var spot = new List<int2>();

                while (toCheck.Length > 0)
                {
                    var nextToCheck = new NativeList<int2>(Allocator.Temp);

                    for (var i = 0; i < toCheck.Length; i++)
                    {
                        var hex = _hexDataLayer[toCheck[i]];
                        spot.Add(hex.Position);
                        hexesAtLayer.Remove(hex.Position);

                        foreach (var neighbor in hex.Neighbors)
                        {
                            if (neighbor.Level.Value == level && hexesAtLayer.Contains(neighbor.Position))
                            {
                                nextToCheck.Add(neighbor.Position);
                                hexesAtLayer.Remove(neighbor.Position);
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

    private IEnumerator DrawBorders()
    {
        while (true)
        {
            var orderedLevels = _hexDataLayer.Hexes
                .Select(hexData => hexData.Level.Value)
                .Distinct()
                .OrderByDescending(level => level);

            foreach (var level in orderedLevels)
            {
                var hexesAtLayer = new NativeHashSet<int2>(_hexDataLayer.Hexes.Count, Allocator.Temp);

                foreach (var hexData in _hexDataLayer.Hexes)
                {
                    if (hexData.Level.Value == level)
                        hexesAtLayer.Add(hexData.Position);
                }

                while (hexesAtLayer.Count > 0)
                {
                    var toCheck = new NativeList<int2>(Allocator.Temp);
                    using var enumerator = hexesAtLayer.GetEnumerator();
                    enumerator.MoveNext();
                    toCheck.Add(enumerator.Current);

                    var spot = new NativeList<int2>(Allocator.Temp);

                    while (toCheck.Length > 0)
                    {
                        var nextToCheck = new NativeList<int2>(Allocator.Temp);

                        for (var i = 0; i < toCheck.Length; i++)
                        {
                            var hex = _hexDataLayer[toCheck[i]];
                            spot.Add(hex.Position);
                            hexesAtLayer.Remove(hex.Position);

                            foreach (var neighbor in hex.Neighbors)
                            {
                                if (neighbor.Level.Value == level && hexesAtLayer.Contains(neighbor.Position))
                                {
                                    nextToCheck.Add(neighbor.Position);
                                    hexesAtLayer.Remove(neighbor.Position);
                                }
                            }
                        }

                        toCheck.Clear();
                        toCheck.AddRange(nextToCheck);
                    }

                    var border = _spotGenerator.BuildLargeSizeBorderLine(spot, 1.3f);
                    var smallBorder = _spotGenerator.BuildSmallSizeBorderLine(spot);

                    smallBorder.SetHeight(0.25f * level);
                    var stripe = new Stripe(72, smallBorder, border);

                    border.Draw(Color.blue, Color.green, 5f);
                    smallBorder.Draw(Color.red, Color.yellow, 5f);

                    stripe.Draw(Color.magenta, 5f);
                }
            }

            yield return new WaitForSeconds(5);
        }
    }


    [Inject]
    private void Init(MountsGenerator mountsGeneratorService,
        WaterGeneratorService waterGeneratorService, TerrainHexViewGenerator surfaceGeneratorService,
        SpotGenerator spotGenerator, HexViewDataLayer hexDataLayer, TerrainLevelGenerator terrainLevelGenerator)
    {
        Debug.Log("[skh] TerrainGeneratorService.Init()");

        _mountsGeneratorService = mountsGeneratorService;
        _waterGeneratorService = waterGeneratorService;
        _surfaceGeneratorService = surfaceGeneratorService;
        _terrainLevelGenerator = terrainLevelGenerator;

        _spotGenerator = spotGenerator;
        _hexDataLayer = hexDataLayer;
    }
}