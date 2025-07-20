using Unity.Mathematics;
using UnityEngine;

public struct FieldsVector
{
    public FieldsVector(int2 gridPosition, Vector3 worldPosition)
    {
        WorldPosition = worldPosition;
        GridPosition = gridPosition;
    }

    public float3 WorldPosition { get; private set; }
    public int2 GridPosition { get; }

    public float Height => WorldPosition.y;

    public FieldsVector CloneWithNewHeight(float height)
    {
        var result = new FieldsVector(GridPosition, new Vector3(WorldPosition.x, height, WorldPosition.z));
        return result;
    }

    #region HexOwner
    public void SetHeight(float height)
    {
        WorldPosition = new float3(WorldPosition.x, height, WorldPosition.z);
    }
    #endregion
}
