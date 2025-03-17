using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class TerrainLevelGenerator : ISurfaceGenerator
{
    private readonly HeightmapDataLayer _heightmapDataLayer;
    private readonly HexViewDataLayer _hexDataLayer;

    private readonly TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;

    public TerrainLevelGenerator(
        HexViewDataLayer hexDataLayer,
        TerrainGeneratorSettingsScriptable terrainGeneratorSettingsScriptable,
        HeightmapDataLayer heightmapDataLayer)
    {
        _hexDataLayer = hexDataLayer;
        _terrainGeneratorSettingsScriptable = terrainGeneratorSettingsScriptable;
        _heightmapDataLayer = heightmapDataLayer;
    }

    public async UniTask<bool> Generate(List<HexId> shape, int level)
    {
        var pixelsPerHex = _terrainGeneratorSettingsScriptable.PixelsPerHex * .6f;
        var pixelsPerUnit = _terrainGeneratorSettingsScriptable.PixelsPerUnit;
        var region = BuildRect(shape);

        var resolution = (int) (region.width * pixelsPerUnit);
        var forceCommand = new ApplyVectorForcesCommand(_hexDataLayer);

        var circleEmitters = new NativeList<CircleEmitter>(Allocator.TempJob);

        var connectedHexes = new HashSet<HexId>();

        for (var i = 0; i < shape.Count; i++)
        {
            var hexData = _hexDataLayer[shape[i]];
            var position = ToTextureSpace(region, resolution, hexData.Position3D.xz);
            var emittersCount = Random.Range(3, 7);

            var emitters = EmitterPacker.PlaceEmitters(position, 20, 5, 8, emittersCount, .4f);

            foreach (var emitterData in emitters)
            {
                circleEmitters.Add(CircleEmitter.FromEmitterData(emitterData, Random.Range(15, 30),
                    (FalloffType) Random.Range(0, 3)));
            }


            connectedHexes.Add(shape[i]);
            var neighbours = HexUtil.Neighbours(shape[i]);

            foreach (var neighbour in neighbours)
            {
                if (shape.Contains(neighbour) && !connectedHexes.Contains(neighbour))
                {
                    var neighbourData = _hexDataLayer[neighbour];
                    var lineBetween = hexData.Position3D.xz - neighbourData.Position3D.xz;

                    position = ToTextureSpace(region, resolution,
                        neighbourData.Position3D.xz + lineBetween * Random.Range(0.4f, 0.6f));
                    var platoRadius = pixelsPerHex * Random.Range(0.3f, 0.5f);

                    var connector = new CircleEmitter
                    {
                        Position = position,
                        PlatoRadius = platoRadius,
                        FalloffRadius = (pixelsPerHex - platoRadius) * Random.Range(1, 1.4f),
                        FalloffType = FalloffType.Smooth
                    };
                    circleEmitters.Add(connector);
                }
            }
        }

        var heightmap = await forceCommand.Execute(resolution, circleEmitters);

        _heightmapDataLayer.SetTexture(heightmap.ToTexture());

        var applyTextureCommand = new ApplyRectTextureToVectorFieldCommand(_hexDataLayer);
        await applyTextureCommand.Execute(region, heightmap, 1);

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

    private Rect BuildRect(List<HexId> shape)
    {
        if (shape.Count == 0)
        {
            Debug.LogError("Shape is null or empty!");
            return Rect.zero;
        }

        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;

        // Determine the bounding box coordinates
        for (var i = 0; i < shape.Count; i++)
        {
            var hexViewData = _hexDataLayer.GetHex(shape[i]);
            if (hexViewData == null)
            {
                Debug.LogWarning($"HexViewData not found for index {i}, skipping.");
                continue;
            }

            var points = hexViewData.Points;
            foreach (var point in points)
            {
                // Assuming point has properties X and Z or similar.
                if (point.Position.x < minX) minX = point.Position.x;
                if (point.Position.z < minY) minY = point.Position.z;
                if (point.Position.x > maxX) maxX = point.Position.x;
                if (point.Position.z > maxY) maxY = point.Position.z;
            }
        }

        minX -= 1;
        minY -= 1;
        maxX += 1;
        maxY += 1;

        // Calculate width and height
        var width = maxX - minX;
        var height = maxY - minY;

        // Determine the size of the square
        var maxSize = Mathf.Max(width, height);

        // Center the shape in the square rect
        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;

        // Define the square rect
        var squareRect = new Rect(
            centerX - maxSize / 2, // Min X
            centerY - maxSize / 2, // Min Y
            maxSize, // Width
            maxSize // Height
        );

        return squareRect;
    }

    public int2 ToTextureSpace(Rect region, int resolution, float2 position) =>
        (int2) math.remap(region.min, region.max, new float2(0, 0), new float2(resolution, resolution),
            position);
}