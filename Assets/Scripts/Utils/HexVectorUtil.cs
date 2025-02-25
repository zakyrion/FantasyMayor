using Unity.Mathematics;
using UnityEngine;

public static class HexVectorUtil
{
    private const float SQRT3 = 1.73205080757f;
    private static readonly float3 _direction = new Vector3(1, 0, SQRT3).normalized;
    public static float TriangleSegmentSize { get; set; }

    public static int2 Neighbour(int index)
    {
        switch (index)
        {
            case 0:
                return new int2(1, 0); // right
            case 1:
                return new int2(1, -1); // right bottom
            case 2:
                return new int2(0, -1); // left bottom 
            case 3:
                return new int2(-1, 0); // left
            case 4:
                return new int2(-1, 1); // left top
            case 5:
                return new int2(0, 1); // right top
            default:
                return default;
        }
    }

    public static float3 CalculateWorldPosition(int2 position)
    {
        var x = TriangleSegmentSize * position.x; // рух по осі Х,
        return new float3(x, 0, 0) + _direction * TriangleSegmentSize * position.y;
    }

    public static int2 CalculateGridPosition(float3 position)
    {
        var step = _direction * TriangleSegmentSize;
        var y = position.z / step.z;

        var x = (position.x - y * step.x) / TriangleSegmentSize;
        //return new int2(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        return new int2((int) math.round(x), (int) math.round(y));
    }

    public static int2 CalculateGridPosition(Rect rect)
    {
        var step = _direction * TriangleSegmentSize;
        var y = rect.center.y / step.z;

        var x = (rect.center.x - y * step.x) / TriangleSegmentSize;
        //return new int2(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        return new int2((int) math.round(x), (int) math.round(y));
    }
}