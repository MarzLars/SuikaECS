using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace SuikaScripts
{
public struct SuikaGameConfig : IComponentData
{
    public uint Seed;
    public int NextSpawnIndex;
}

public struct SuikaBurstConfigBuffer : IBufferElementData
{
    public int SphereCount;
    public Entity SpherePrefabEntity;
    public float SphereSize;
    public float Radius;
    public float SleepDelaySeconds;

    public static SuikaBurstConfigBuffer Default => new SuikaBurstConfigBuffer
    {
        SphereCount = 8,
        SpherePrefabEntity = Entity.Null,
        SphereSize = 1f,
        Radius = 0.6f,
        SleepDelaySeconds = 3f,
    };
}

[MaterialProperty("_BaseColor")]
public struct SuikaColorOverride : IComponentData
{
    public float4 Value;
}

/// <summary>
/// Buffer containing all prefab tier definitions.
/// This allows flexible, data-driven configuration of any number of tiers.
/// </summary>
public struct SuikaPrefabTierBuffer : IBufferElementData
{
    public byte Tier;
    public byte Shape;
    public Entity PrefabEntity;
    public float Scale;
    public float4 Color;
    public int ScoreValue;
    public byte BurstOnMerge;
    public int BurstConfigIndex;
}

public struct DropperSpawnPoint : IComponentData
{
    public float3 Position;
}


public struct DropperInitialSpawnRequest : IComponentData
{
    public int Count;
}

public struct DropperStressTestConfig : IComponentData
{
    public byte Enabled;
    public float SpawnIntervalSeconds;
    public float TimeSinceLastSpawnSeconds;
    public int SpawnCountPerInterval;
}



}
