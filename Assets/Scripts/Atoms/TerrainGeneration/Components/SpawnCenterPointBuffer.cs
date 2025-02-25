using HW.Authoring;
using Unity.Entities;

public struct SpawnCenterPointBuffer : IHexSpawnerComponent
{
    public Entity Prefab { get; set; }
}

public struct SpawnCornerPointBuffer : IHexSpawnerComponent
{
    public Entity Prefab { get; set; }
}

public struct SpawnHexViewBuffer : IHexSpawnerComponent
{
    public Entity Prefab { get; set; }
}