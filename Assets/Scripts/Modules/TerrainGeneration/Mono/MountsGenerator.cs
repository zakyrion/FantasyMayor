using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Atoms.Hexes.DataLayer;
using Core.DataLayer;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Zenject;

public class MountsGenerator : ISurfaceGenerator
{
    private readonly HeightMapsGenerator _heightMapsGenerator;
    private readonly HeightsGeneratorSettingsScriptable _heightsGeneratorSettings;
    private readonly HexViewDataLayer _hexDataLayer;

    private readonly IDataContainer<SeedDataLayer> _seedDataLayer;
    private readonly SpotGenerator _spotGenerator;
    private readonly TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;

    [Inject]
    public MountsGenerator(IDataContainer<SeedDataLayer> seedDataLayer, HexViewDataLayer hexDataLayer,
        TerrainGeneratorSettingsScriptable terrainGeneratorSettingsScriptable, HeightMapsGenerator heightMapsGenerator,
        HeightsGeneratorSettingsScriptable heightsGeneratorSettings, SpotGenerator spotGenerator)
    {
        _seedDataLayer = seedDataLayer;
        _hexDataLayer = hexDataLayer;
        _terrainGeneratorSettingsScriptable = terrainGeneratorSettingsScriptable;
        _heightMapsGenerator = heightMapsGenerator;
        _heightsGeneratorSettings = heightsGeneratorSettings;
        _spotGenerator = spotGenerator;
    }

    public async UniTask AddHillAsync(List<HexId> shape, CancellationToken cancellationToken)
    {
        var seedResult = await _seedDataLayer.GetAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested || !seedResult.Exist)
        {
            return;
        }

        var seedData = seedResult.DataLayer;

        var applyTextureCommand = new ApplyRectTextureToVectorFieldCommand(_hexDataLayer);
        var regionCommand = new CreateRegionForCommand(_terrainGeneratorSettingsScriptable.DecorationMapResolution);
        var blendCommand = new MapsCombineCommand();

        var settingsHill = _heightsGeneratorSettings.GetRandomSettings(SurfaceType.Hill);
        var settingsMount = _heightsGeneratorSettings.GetRandomSettings(SurfaceType.Mountain);
        var settingsBlend = _heightsGeneratorSettings.GetRandomSettings(SurfaceType.Blend);

        var spotHill = await _spotGenerator.GenerateSpot(shape);

        var regionHill = await regionCommand.GetHeightmap(spotHill, .4f);
        var regionHillTexture = regionHill.ToTexture();

        var heightmapHill = _heightMapsGenerator
            .Generate(settingsHill, seedData.Seed, regionHill.Resolution).ToTexture();
        var heightmapMount = _heightMapsGenerator
            .Generate(settingsMount, seedData.Seed, regionHill.Resolution).ToTexture();
        var blendMap = _heightMapsGenerator.Generate(settingsBlend, seedData.Seed, regionHill.Resolution)
            .ToTexture();

        var blendedTexture =
            blendCommand.BlendMaps(heightmapHill, heightmapMount, blendMap);

        blendedTexture = blendCommand.MultiplyMaps(regionHillTexture, blendedTexture);

        await applyTextureCommand.Execute(spotHill.Rect, blendedTexture.ToTerrainHeightmap(Allocator.TempJob),
            _terrainGeneratorSettingsScriptable.HexSize);

        var hexVectors = _hexDataLayer.HexVectors;

        foreach (var hexData in _hexDataLayer.Hexes)
        {
            var vertices = hexData.Vertices;
            for (var i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                var gridPosition = HexVectorUtil.CalculateGridPosition(vertex);
                vertices[i] = hexVectors[gridPosition].WorldPosition;
            }

            hexData.SetVertices(vertices);
        }
    }

    public async void AddMount(List<HexId> shape)
    {
        var applyTextureCommand = new ApplyRectTextureToVectorFieldCommand(_hexDataLayer);
        var regionCommand = new CreateRegionForCommand(1024);
        var blendCommand = new MapsCombineCommand();
        var smoothCommand = new SmoothVectorFieldCommand(_hexDataLayer);
        var toMeshCommand = new VectorFieldToMeshesCommand(_hexDataLayer);

        var shapeMount = GetSubShape(shape, SurfaceType.Mountain);

        var settingsHill = _heightsGeneratorSettings.GetRandomSettings(SurfaceType.Hill);
        var settingsMount = _heightsGeneratorSettings.GetRandomSettings(SurfaceType.Mountain);
        var settingsBlend = _heightsGeneratorSettings.GetRandomSettings(SurfaceType.Blend);

        var spotHill = await _spotGenerator.GenerateSpot(shape);
        var spotMount = await _spotGenerator.GenerateSpot(shapeMount, spotHill.Rect);

        var regionHill = await regionCommand.GetHeightmap(spotHill);
        var regionMount = await regionCommand.GetHeightmap(spotMount, 1f);
        var blendedRegion = blendCommand.AddMaps(regionHill.ToTexture(), regionMount.ToTexture(), true);

        var heightmapHill = _heightMapsGenerator.Generate(settingsHill, 1234, regionHill.Resolution);
        var heightmapMount = _heightMapsGenerator.Generate(settingsMount, 1234, regionHill.Resolution);
        var blendMap = _heightMapsGenerator.Generate(settingsBlend, 1234, regionHill.Resolution);

        var blendedTexture =
            blendCommand.BlendMaps(heightmapHill.ToTexture(), heightmapMount.ToTexture(), blendMap.ToTexture());

        blendedTexture = blendCommand.MultiplyMaps(blendedRegion, blendedTexture);

        await applyTextureCommand.Execute(spotHill.Rect, blendedTexture.ToTerrainHeightmap(Allocator.TempJob),
            _terrainGeneratorSettingsScriptable.HexSize);
        await smoothCommand.Execute();

        toMeshCommand.Execute();
    }

    public async UniTask<bool> Generate(dynamic data)
    {
        return true;
    }

    private List<HexId> GetSubShape(List<HexId> shape, SurfaceType type)
    {
        return shape.Where(c => _hexDataLayer[c].SurfaceType == type).ToList();
    }
}
