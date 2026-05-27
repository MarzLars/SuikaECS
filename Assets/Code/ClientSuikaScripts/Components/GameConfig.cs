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

    public struct SuikaPopConfigBuffer : IBufferElementData
    {
        public int SphereCount;
        public Entity SpherePrefabEntity;
        public float SphereSize;
        public float Radius;
        public float SleepDelaySeconds;

        public static SuikaPopConfigBuffer Default => new() {
            SphereCount = 8,
            SpherePrefabEntity = Entity.Null,
            SphereSize = 1f,
            Radius = 0.6f,
            SleepDelaySeconds = 3f
        };
    }

    [MaterialProperty("_BaseColor")]
    public struct SuikaColorOverride : IComponentData
    {
        public float4 Value;
    }

    /// <summary>
    ///     Buffer containing all prefab tier definitions.
    ///     This allows flexible, data-driven configuration of any number of tiers.
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

    public struct DropperAimConfig : IComponentData
    {
        public float MoveSpeed;
        public float MinX;
        public float MaxX;
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

    public struct DropperRaycastVisualConfig : IComponentData
    {
        public float MaxDistance;
        public int LayerMask;
    }

    public struct DropperPreviewEntity : IComponentData
    {
        public Entity Value;
    }

    public struct DropperPreviewState : IComponentData
    {
        public byte Tier;
        public byte Shape;
    }

    public struct DropperPreviewReady : IComponentData, IEnableableComponent
    { }

    public struct DropperPreviewTag : IComponentData
    { }

// When an item is spawned we add DroppedAwaitCollision to it so systems can wait
// for its first collision before allowing further drops.
    public struct DroppedAwaitCollision : IComponentData
    {
        public Entity OwnerConfigEntity;
    }

// Marker on the config entity to block further spawn until collision occurs.
    public struct DropperSpawnBlocked : IComponentData
    { }
}