using DataTypes;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct Spot
{
    public NativeArray<SpotSegment> BorderLine;
    public float3 PointInside;
    public Rect Rect;
}

public struct SpotLines
{
    public NativeArray<Line> BorderLine;
    public float3 PointInside;
    public Rect Rect;
}