using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Zenject;

public class TerrainLevelGenerator : ISurfaceGenerator
{
    private readonly HeightmapDataLayer _heightmapDataLayer;
    private readonly HeightMapsGenerator _heightMapsGenerator;
    private readonly HeightsGeneratorSettingsScriptable _heightsGeneratorSettings;
    private readonly HexViewDataLayer _hexDataLayer;

    private readonly SeedDataLayer _seedDataLayer;
    private readonly RegionShapeShiftSettingsScriptable _shiftSettings;
    private readonly SpotGenerator _spotGenerator;
    private readonly TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;

    [Inject]
    public TerrainLevelGenerator(HeightMapsGenerator heightMapsGenerator,
        HeightsGeneratorSettingsScriptable heightsGeneratorSettings,
        RegionShapeShiftSettingsScriptable regionShapeShiftSettings,
        HexViewDataLayer hexDataLayer,
        SeedDataLayer seedDataLayer,
        SpotGenerator spotGenerator,
        TerrainGeneratorSettingsScriptable terrainGeneratorSettingsScriptable,
        HeightmapDataLayer heightmapDataLayer)
    {
        _heightMapsGenerator = heightMapsGenerator;
        _heightsGeneratorSettings = heightsGeneratorSettings;
        _hexDataLayer = hexDataLayer;
        _seedDataLayer = seedDataLayer;
        _spotGenerator = spotGenerator;
        _terrainGeneratorSettingsScriptable = terrainGeneratorSettingsScriptable;
        _heightmapDataLayer = heightmapDataLayer;
        _shiftSettings = regionShapeShiftSettings;
    }

    public async UniTask<bool> Generate(NativeList<int2> shape, int level)
    {
        var createLevelCommand =
            new CreateLevelsForRegionCommand(_terrainGeneratorSettingsScriptable.DecorationMapResolution);
        var applyTextureCommand = new ApplyRectTextureToVectorFieldCommand(_hexDataLayer);


        var stripe = GenerateIsolines(shape, level, 2);
        stripe.Draw(Color.magenta);

        Debug.Log("[skh] apply level");
        var maxHeight = _terrainGeneratorSettingsScriptable.HeightPerLevel * level;

        var heightmap =
            await createLevelCommand.Execute(stripe, stripe.GetPointInside().xz, maxHeight);
        var heightmapTexture = heightmap.ToTexture();

        _heightmapDataLayer.SetTexture(heightmapTexture);

        await applyTextureCommand.Execute(stripe.Rect(), heightmapTexture, level);

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

        return true;
    }

    private Stripe GenerateIsolines(NativeList<int2> shape, int level, int linesCount)
    {
        
        //спробувати перлін штампи.
        //ізолінія окреслює регіон, на який потім наноситься штамп перліна, якщо шум безперервно заповнює регіон всередині на певний відосток
        //тоді по котнуру будується "справжня" ізолінія
        //для більшої ізолінії також використати попередній регіон щоб бути впевненим що він більше за попередній
        
        //1 - використати сплайн
        //2 - вектор напрям для зміщення ліній в певну сторону
        //3 - обмеження мінімальної відстані між точками ізоліній 
        
        var smallBorder = _spotGenerator.BuildSmallSizeBorderLine(shape);
        var bigBorder = _spotGenerator.BuildLargeSizeBorderLine(shape, 1.3f);

        smallBorder.SetHeight(1);
        bigBorder.SetHeight(0);

        var stripe = new Stripe(50 * shape.Length, smallBorder, bigBorder);
        stripe.ShiftLine(0, _shiftSettings.GetRandomSetting(), .3f);
        stripe.ShiftLine(1, _shiftSettings.GetRandomSetting(), .3f);

        return stripe;
    }
}