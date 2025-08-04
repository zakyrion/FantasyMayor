using System.Collections.Generic;
using Unity.Mathematics;

public class HexPointData
{
    private HexPointJobsData _data;
    public List<HexPointData> ConnectedPoints = new();
    public List<int2> Owners = new();

    public float3 Position
    {
        get => _data.Position;
        set => _data.Position = value;
    }

    public PointType Type
    {
        get => _data.Type;
        set => _data.Type = value;
    }

    public void AddOwner(int2 owner)
    {
        if (Owners.Contains(owner)) return;

        Owners.Add(owner);
    }

    public void AddConnectedPoint(HexPointData point)
    {
        if (ConnectedPoints.Contains(point)) return;

        ConnectedPoints.Add(point);
    }

    public bool AtTheSamePosition(float3 position) => math.distance(Position, position) < 0.01f;

    public static implicit operator HexPointJobsData(HexPointData data) => data._data;
}

public struct HexPointJobsData
{
    public float3 Position;
    public PointType Type;
}