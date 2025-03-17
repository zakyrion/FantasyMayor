using System.Collections.Generic;
using Unity.Mathematics;

/// <summary>
/// This class is responsible for spawning points on a hexagonal grid.
/// </summary>
public class SpawnPointsCommand
{
    private readonly HexViewDataLayer _hexDataLayer;

    public SpawnPointsCommand(HexViewDataLayer hexDataLayer)
    {
        _hexDataLayer = hexDataLayer;
    }

    /// <summary>
    /// Executes the command to spawn points on the hexagonal grid.
    /// </summary>
    /// <param name="hexSize">The size of the hexagon.</param>
    public void Execute(float hexSize)
    {
        var hexes = _hexDataLayer.Hexes;
        var points = new List<HexPointData>();

        foreach (var hex in hexes)
        {
            var mesh = HexMeshUtil.CreatePointyBasedHex(hexSize, hex.Position3D);
            hex.SetMesh(mesh);
            var vertices = mesh.vertices;

            for (var i = 1; i < vertices.Length; i++)
            {
                var existedPoint = points.Find(point => point.AtTheSamePosition(vertices[i]));
                if (existedPoint != null)
                {
                    existedPoint.AddOwner(hex.HexId.Coords);
                    hex.AddPoint(existedPoint);
                    continue;
                }

                existedPoint = new HexPointData
                {
                    Position = vertices[i],
                    Type = PointType.Corner
                };
                existedPoint.AddOwner(hex.HexId.Coords);
                hex.AddPoint(existedPoint);
                points.Add(existedPoint);
            }
        }

        foreach (var hex in hexes)
        {
            foreach (var point in hex.Points)
            {
                foreach (var owner in point.Owners)
                {
                    if (hex.HexId.Equals(owner))
                    {
                        continue;
                    }

                    var neighbour = _hexDataLayer[owner];
                    foreach (var neighbourPoint in neighbour.Points)
                    {
                        if (neighbourPoint.Type == PointType.Center)
                        {
                            continue;
                        }

                        var distance = math.distance(point.Position, neighbourPoint.Position);
                        if (distance > .01f && distance < hexSize * 1.05f)
                        {
                            point.AddConnectedPoint(neighbourPoint);
                            neighbourPoint.AddConnectedPoint(point);
                        }
                    }
                }
            }
        }
    }
}