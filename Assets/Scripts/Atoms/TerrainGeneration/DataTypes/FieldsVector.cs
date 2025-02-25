using System;
using Unity.Mathematics;
using UnityEngine;

public struct FieldsVector
{
    public FieldsVector(int2 gridPosition, Vector3 worldPosition, int2 hexOwner1)
    {
        WorldPosition = worldPosition;
        GridPosition = gridPosition;

        MainOwner = hexOwner1;
        OwnerCount = 1;

        _hexOwner2 = default;
        _hexOwner3 = default;
    }

    public float3 WorldPosition { get; private set; }
    public int2 GridPosition { get; }
    
    public float Height => WorldPosition.y;

    public FieldsVector CloneWithNewHeight(float height)
    {
        var result = new FieldsVector(GridPosition, new Vector3(WorldPosition.x, height, WorldPosition.z), MainOwner);

        for (var i = 1; i < OwnerCount; i++) result.AddOwner(GetOwner(i));

        return result;
    }

    #region HexOwner

    public void AddOwner(int2 owner)
    {
        if (MainOwner.Equals(owner) || _hexOwner2.Equals(owner) || _hexOwner3.Equals(owner)) return;

        switch (OwnerCount)
        {
            case 1:
                _hexOwner2 = owner;
                break;
            case 2:
                _hexOwner3 = owner;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        OwnerCount++;
    }

    public void SetHeight(float height)
    {
        WorldPosition = new float3(WorldPosition.x, height, WorldPosition.z);
    }

    public int2 GetOwner(int i)
    {
        switch (i)
        {
            case 0: return MainOwner;
            case 1: return _hexOwner2;
            case 2: return _hexOwner3;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    public int2 GetLastOwner()
    {
        switch (OwnerCount)
        {
            case 1: return MainOwner;
            case 2: return _hexOwner2;
            case 3: return _hexOwner3;
            default: throw new ArgumentOutOfRangeException();
        }
    }

    private int2 _hexOwner2;
    private int2 _hexOwner3;
    public int2 MainOwner { get; }

    public int OwnerCount { get; private set; }

    #endregion
}