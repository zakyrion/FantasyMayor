using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DataTypes;
using Dreamteck.Splines;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using VContainer;

public class SpotGenerator : MonoBehaviour
{
    [SerializeField] private SplineComputer _splineComputer;

    private HexViewDataLayer _hexDataLayer;
    private TerrainGeneratorSettingsScriptable _terrainGeneratorSettingsScriptable;

    [Inject]
    public void Init(HexViewDataLayer hexDataLayer,
        TerrainGeneratorSettingsScriptable terrainGeneratorSettingsScriptable)
    {
        _hexDataLayer = hexDataLayer;
        _terrainGeneratorSettingsScriptable = terrainGeneratorSettingsScriptable;
    }

    public async UniTask<Spot> GenerateRiverSpot(List<int2> hexes,
        Allocator allocator = Allocator.TempJob)
    {
        var beginHex = _hexDataLayer[hexes[0]];

        var borderEdges = HexUtil.Neighbours(beginHex.Position).Where(pos => !_hexDataLayer.Exists(pos)).Select(n =>
            HexUtil.GetCenterOfEdge(n - beginHex.Position,
                beginHex.Position3D, _terrainGeneratorSettingsScriptable.HexSize)).ToList();

        //побудувати сплайн по точках, центр грані, центр гекса, центр грані, центр гекса
        //по кожній точці сплайну взяти дотичну і по ній подубувати випадкову точку русла
        //зробити обхід по часовій стрілці, зберігаючи точки русла
        //обʼєднати точки русла в замкнений сплайн,по сплайну побудувати spot

        //Можна побудувати криву, сплайн між всіма точками, а потім взяти паралельну кожній точці і побудувати русло


        return default;
    }

    public async UniTask<Spot> GenerateSpot(List<int2> hexes,
        Allocator allocator = Allocator.TempJob)
    {
        var line = BuildFullSizeBorderLine(hexes.ToNativeList(Allocator.Temp));
        var splinePoints = new List<SplinePoint>();

        var minX = line.Segments.Min(point => point.Start.x) - _terrainGeneratorSettingsScriptable.HexSize * .5f;
        var maxX = line.Segments.Max(point => point.Start.x) + _terrainGeneratorSettingsScriptable.HexSize * .5f;
        var minZ = line.Segments.Min(point => point.Start.z) - _terrainGeneratorSettingsScriptable.HexSize * .5f;
        var maxZ = line.Segments.Max(point => point.Start.z) + _terrainGeneratorSettingsScriptable.HexSize * .5f;

        splinePoints.AddRange(line.Segments.Select(point => new SplinePoint
        {
            position = point.Start,
            normal = Vector3.up,
            size = 1f,
            color = Color.white
        }));

        splinePoints.Add(splinePoints[0]);
        _splineComputer.SetPoints(splinePoints.ToArray());

        await UniTask.WaitForSeconds(.05f);

        var segments = GenerateSpot(allocator);

        return new Spot
        {
            BorderLine = segments,
            Rect = new Rect(minX, minZ, maxX - minX, maxZ - minZ),
            PointInside = _hexDataLayer[hexes[0]].Position3D
        };
    }

    public async UniTask<Spot> GenerateSpot(List<int2> hexes, Rect rect,
        Allocator allocator = Allocator.TempJob)
    {
        var line = BuildFullSizeBorderLine(hexes.ToNativeList(Allocator.Temp));
        var splinePoints = new List<SplinePoint>();

        splinePoints.AddRange(line.Segments.Select(point => new SplinePoint
        {
            position = point.Start,
            normal = Vector3.up,
            size = 1f,
            color = Color.white
        }));

        splinePoints.Add(splinePoints[0]);
        _splineComputer.SetPoints(splinePoints.ToArray());

        await UniTask.WaitForSeconds(.05f);

        var segments = GenerateSpot(allocator);

        return new Spot
        {
            BorderLine = segments,
            Rect = rect,
            PointInside = _hexDataLayer[hexes[0]].Position3D
        };
    }

    public BorderLine BuildFullSizeBorderLine(NativeList<int2> hexes)
    {
        var lines = new List<LineWithCenter>();

        foreach (var hexPosition in hexes)
        {
            var hex = _hexDataLayer[hexPosition];

            for (var i = 0; i < 6; i++)
            {
                var neighborPosition = HexUtil.Neighbour(i, hexPosition);

                if (!_hexDataLayer.Exists(neighborPosition) ||
                    !hexes.Contains(neighborPosition)) lines.Add(hex.GetLine(i));
            }
        }

        var resultBorderLine = new BorderLine();
        var currentLine = lines[0];
        resultBorderLine.AddLine(currentLine);
        lines.RemoveAt(0);

        var needToContinue = true;

        while (needToContinue)
        {
            var isFound = false;

            foreach (var lineToCheck in lines)
            {
                if (currentLine.Line.CanBeSegment(lineToCheck))
                {
                    isFound = true;
                    currentLine = lineToCheck;
                    resultBorderLine.AddLine(lineToCheck);
                    break;
                }
            }

            if (!isFound)
                needToContinue = false;
            else
                lines.Remove(currentLine);
        }

        return resultBorderLine.Order();
    }

    public BorderLine BuildSmallSizeBorderLine(BorderLine currentBorder, float scale = 0.8f)
    {
        var resultBorder = new BorderLine();

        for (var i = 0; i < currentBorder.Segments.Count; i++)
        {
            var currentSegment = currentBorder.Segments[i];
            var nextSegment = i == currentBorder.Segments.Count - 1
                ? currentBorder.Segments[0]
                : currentBorder.Segments[i + 1];

            var start = math.lerp(currentSegment.CenterOfHexTriangle, currentSegment.Start, scale);
            var end = math.lerp(currentSegment.CenterOfHexTriangle, currentSegment.End, scale);

            var line = new LineWithCenter(currentSegment.CenterOfHexTriangle, new Line(start, end), true);
            resultBorder.AddLine(line);

            if (!currentSegment.HaveSameCenter(nextSegment))
            {
                var points = new List<Tuple<float, float3, float3>>
                {
                    new(math.distance(nextSegment.Start, currentSegment.End),
                        math.lerp(nextSegment.CenterOfHexTriangle, nextSegment.Start, scale),
                        math.lerp(currentSegment.CenterOfHexTriangle, currentSegment.End, scale)),

                    new(math.distance(nextSegment.End, currentSegment.End),
                        math.lerp(nextSegment.CenterOfHexTriangle, nextSegment.End, scale),
                        math.lerp(currentSegment.CenterOfHexTriangle, currentSegment.End, scale)),

                    new(math.distance(nextSegment.End, currentSegment.Start),
                        math.lerp(nextSegment.CenterOfHexTriangle, nextSegment.End, scale),
                        math.lerp(currentSegment.CenterOfHexTriangle, currentSegment.Start, scale)),

                    new(math.distance(nextSegment.Start, currentSegment.Start),
                        math.lerp(nextSegment.CenterOfHexTriangle, nextSegment.Start, scale),
                        math.lerp(currentSegment.CenterOfHexTriangle, currentSegment.Start, scale))
                };

                points = points.OrderBy(p => p.Item1).ToList();

                var center = math.lerp(currentSegment.CenterOfHexTriangle, nextSegment.CenterOfHexTriangle, 0.5f);

                var lineToCenter = new LineWithCenter(center, new Line(points[0].Item2, points[0].Item3), true);
                resultBorder.AddLine(lineToCenter);
            }
        }

        return resultBorder.Order();
    }

    public BorderLine BuildSmallSizeBorderLine(NativeList<int2> hexes, float scale = 0.8f) =>
        BuildSmallSizeBorderLine(BuildFullSizeBorderLine(hexes), scale);

    public BorderLine BuildLargeSizeBorderLine(NativeList<int2> hexes, float scale = 1.2f) =>
        BuildLargeSizeBorderLine(BuildFullSizeBorderLine(hexes), scale);

    public BorderLine BuildLargeSizeBorderLine(BorderLine currentBorder, float scale = 1.2f)
    {
        var resultBorder = new BorderLine();
        var currentSegment = currentBorder.Segments[0];

        var start = currentSegment.CenterOfHexTriangle +
                    (currentSegment.Start - currentSegment.CenterOfHexTriangle) * scale;
        var end = currentSegment.CenterOfHexTriangle +
                  (currentSegment.End - currentSegment.CenterOfHexTriangle) * scale;

        currentSegment = new LineWithCenter(currentSegment.CenterOfHexTriangle, new Line(start, end), true);

        for (var i = 0; i < currentBorder.Segments.Count; i++)
        {
            if (i == currentBorder.Segments.Count - 1)
            {
                var firstSegment = resultBorder.Segments[0];
                resultBorder.AddLine(new LineWithCenter(currentSegment.CenterOfHexTriangle, currentSegment.Start,
                    firstSegment.Start));
            }
            else
            {
                var nextSegment = currentBorder.Segments[i + 1];
                var nextStart = nextSegment.CenterOfHexTriangle +
                                (nextSegment.Start - nextSegment.CenterOfHexTriangle) * scale;
                var nextEnd = nextSegment.CenterOfHexTriangle +
                              (nextSegment.End - nextSegment.CenterOfHexTriangle) * scale;

                var newNextSegment =
                    new LineWithCenter(nextSegment.CenterOfHexTriangle, new Line(nextStart, nextEnd), true);

                if (!currentSegment.CanBeSegment(newNextSegment))
                {
                    var intersection = currentSegment.Intersection(newNextSegment);
                    currentSegment = new LineWithCenter(currentSegment.CenterOfHexTriangle, currentSegment.Start,
                        intersection, true);

                    newNextSegment =
                        new LineWithCenter(nextSegment.CenterOfHexTriangle, intersection, newNextSegment.End, true);
                }

                resultBorder.AddLine(currentSegment);

                currentSegment = newNextSegment;
            }
        }

        return resultBorder.Order();
    }

    private NativeArray<SpotSegment> GenerateSpot(Allocator allocator = Allocator.TempJob)
    {
        var segmentsCount = 360 * 3;
        var result = new NativeArray<SpotSegment>(segmentsCount, allocator);

        var splineDelta = 1f / segmentsCount;

        for (var i = 0; i < segmentsCount; i++)
        {
            var position = _splineComputer.EvaluatePosition(i * splineDelta);

            result[i] = new SpotSegment
            {
                Position = position
            };
        }

        return result;
    }
}