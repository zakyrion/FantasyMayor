using HW.Data;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;

public struct HexPositionComponent : IComponentData
{
    [CreateProperty] public int2 Position;

    public HexPositionComponent(int q, int r)
    {
        Position = new int2(q, r);
    }

    public HexPositionComponent(int2 position)
    {
        Position = position;
    }

    public int Q => Position.x;
    public int R => Position.y;

    public float3 CalculateTopAngleWorldPosition(float size)
    {
        var posX = size * math.sqrt(3) * (Q + (float) R / 2);
        var posY = size * 3f / 2f * R;
        return new float3(posX, 0, posY);
    }

    public float3 CalculateTopAngleWorldPosition()
    {
        var size = MapSpawnSettings.HEX_SIZE;
        var posX = size * math.sqrt(3) * (Q + (float) R / 2);
        var posY = size * 3f / 2f * R;
        return new float3(posX, 0, posY);
    }
}