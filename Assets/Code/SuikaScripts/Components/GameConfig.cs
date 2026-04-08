using Unity.Entities;
using Unity.Mathematics;

public struct SuikaGameConfig : IComponentData
{
    public uint Seed;
    public int NextSpawnIndex;
    public Entity Sphere0PrefabEntity;
    public Entity Sphere1PrefabEntity;
    public Entity Sphere2PrefabEntity;
    public Entity Cylinder3PrefabEntity;
    public Entity Cylinder4PrefabEntity;
    public Entity Cylinder5PrefabEntity;
}

public struct DropperSpawnPoint : IComponentData
{
    public float3 Position;
}


public struct DropperInitialSpawnRequest : IComponentData
{
    public int Count;
}
