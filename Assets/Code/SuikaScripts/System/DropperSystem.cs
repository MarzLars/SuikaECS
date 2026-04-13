using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace SuikaScripts
{
    [BurstCompile]
    public static class DropperSpawnSequenceService
    {
        public const uint DefaultSeed = 123456789u;
        public const byte MinTier = 0;
        public const byte SphereShape = 0;
        public const byte CylinderShape = 1;
        public const byte SphereMaxTier = 2;
        public const byte CylinderMinTier = 3;
        public const byte CylinderMaxTier = 5;
        const uint SeedWeight = 31u;
        const uint SpawnIndexWeight = 17u;

        [BurstCompile]
        public static byte GetTier(uint seed, int spawnIndex)
        {
            uint safeSeed = seed == 0 ? 
                DefaultSeed :
                seed;
            uint safeSpawnIndex = (uint)math.max(spawnIndex, 0);
            const uint range = SphereMaxTier + 1;

            uint mixed = safeSeed * SeedWeight + safeSpawnIndex * SpawnIndexWeight;
            return (byte)(MinTier + (mixed % range));
        }

        [BurstCompile]
        public static byte PromoteTier(byte tier, byte maxTier)
        {
            return (byte)math.min(tier + 1, maxTier);
        }

        [BurstCompile]
        public static float GetScale(byte tier)
        {
            return tier switch
            {
                2 => 0.75f,
                1 => 0.50f,
                _ => 0.25f
            };
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct DropperSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SuikaGameConfig>();
            state.RequireForUpdate<SuikaPrefabTierBuffer>();
            state.RequireForUpdate<DropperSpawnPoint>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Handle stress test auto-spawning
            foreach (var (stressConfig, dropperEntity) in 
                     SystemAPI.Query<RefRW<DropperStressTestConfig>>()
                         .WithEntityAccess())
            {
                if (stressConfig.ValueRO.Enabled == 0)
                    continue;

                stressConfig.ValueRW.TimeSinceLastSpawnSeconds += deltaTime;
                if (stressConfig.ValueRO.TimeSinceLastSpawnSeconds >= stressConfig.ValueRO.SpawnIntervalSeconds)
                {
                    stressConfig.ValueRW.TimeSinceLastSpawnSeconds = 0f;
                    ecb.AddComponent(dropperEntity, new DropperInitialSpawnRequest
                    {
                        Count = stressConfig.ValueRO.SpawnCountPerInterval
                    });
                }
            }

            // Handle manual spawn requests
            if (!SystemAPI.TryGetSingletonEntity<DropperInitialSpawnRequest>(out var requestEntity))
                return;

            var request = SystemAPI.GetComponent<DropperInitialSpawnRequest>(requestEntity);
            int spawnCount = math.max(0, request.Count);

            // Consume the request this frame; spawning work records into the same ECB afterwards.
            ecb.RemoveComponent<DropperInitialSpawnRequest>(requestEntity);

            if (spawnCount == 0)
            {
                return;
            }

            if (!SystemAPI.TryGetSingletonEntity<SuikaGameConfig>(out var configEntity))
                return;
            var config = SystemAPI.GetSingletonRW<SuikaGameConfig>();
            if (!SystemAPI.TryGetSingletonEntity<DropperSpawnPoint>(out var dropperSpawnEntity))
                return;
            var dropperPosition = SystemAPI.GetComponent<DropperSpawnPoint>(dropperSpawnEntity).Position;
            var tierBuffer = SystemAPI.GetBuffer<SuikaPrefabTierBuffer>(configEntity);

            int startSpawnIndex = config.ValueRO.NextSpawnIndex;
            uint seed = config.ValueRO.Seed;

            // Reserve indices on main thread to maintain determinism for potential concurrent requests
            config.ValueRW.NextSpawnIndex += spawnCount;

            state.Dependency = new DropperSpawnParallelJob
            {
                ECB = ecb.AsParallelWriter(),
                TierBuffer = tierBuffer,
                DropperPosition = dropperPosition,
                Seed = seed,
                StartSpawnIndex = startSpawnIndex
            }.Schedule(spawnCount, 64, state.Dependency);
        }

        [BurstCompile]
        public struct DropperSpawnParallelJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public DynamicBuffer<SuikaPrefabTierBuffer> TierBuffer;
            public float3 DropperPosition;
            public uint Seed;
            public int StartSpawnIndex;

            public void Execute(int index)
            {
                int currentSpawnIndex = StartSpawnIndex + index;
                byte tier = DropperSpawnSequenceService.GetTier(Seed, currentSpawnIndex);
                float sphereScale = DropperSpawnSequenceService.GetScale(tier);
                float cylinderScaleXY = 1f;

                if (!TryGetTierDefinition(TierBuffer, tier, out var tierDefinition))
                {
                    return;
                }

                byte shape = tierDefinition.Shape;

                if (shape == DropperSpawnSequenceService.SphereShape)
                {
                    sphereScale = tierDefinition.Scale;
                }
                else
                {
                    cylinderScaleXY = tierDefinition.Scale;
                }

                if (tierDefinition.PrefabEntity == Entity.Null)
                    return;

                var spawnedItem = ECB.Instantiate(index, tierDefinition.PrefabEntity);
                
                // Baked components (SuikaItem, SuikaColorOverride) are already present on the prefab.
                // We only need to update the instance-specific data.
                
                ECB.SetComponent(index, spawnedItem, new SuikaColorOverride { Value = tierDefinition.Color });
                ECB.SetComponent(index, spawnedItem, LocalTransform.FromPositionRotationScale(
                    DropperPosition,
                    quaternion.identity,
                    shape == DropperSpawnSequenceService.SphereShape
                        ? sphereScale
                        : 1f));

                // Cylinders: scale XY only, keep Z unchanged.
                ECB.RemoveComponent<PostTransformMatrix>(index, spawnedItem);
                if (shape == DropperSpawnSequenceService.CylinderShape)
                {
                    ECB.AddComponent(index, spawnedItem, new PostTransformMatrix
                    {
                        Value = float4x4.Scale(cylinderScaleXY, cylinderScaleXY, 1f)
                    });
                }

                ECB.SetComponent(index, spawnedItem, new SuikaItem
                {
                    Tier = tier,
                    Shape = shape,
                    SpawnIndex = currentSpawnIndex,
                    CanMerge = 1
                });
            }

            static bool TryGetTierDefinition(DynamicBuffer<SuikaPrefabTierBuffer> tierBuffer, byte tier, out SuikaPrefabTierBuffer tierDefinition)
            {
                for (int i = 0; i < tierBuffer.Length; i++)
                {
                    if (tierBuffer[i].Tier == tier)
                    {
                        tierDefinition = tierBuffer[i];
                        return true;
                    }
                }

                tierDefinition = default;
                return false;
            }
        }

    }
}