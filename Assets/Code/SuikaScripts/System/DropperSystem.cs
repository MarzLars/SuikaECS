using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace SuikaScripts
{
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

        public static byte PromoteTier(byte tier, byte maxTier)
        {
            return (byte)math.min(tier + 1, maxTier);
        }

        public static float GetScale(byte tier)
        {
            return tier switch
            {
                2 => 0.75f,
                1 => 0.50f,
                _ => 0.25f
            };
        }

        public static bool TryGetTierDefinition(DynamicBuffer<SuikaPrefabTierBuffer> tierBuffer, byte tier, out SuikaPrefabTierBuffer tierDefinition)
        {
            for (int i = 0; i < tierBuffer.Length; i++) {
                if (tierBuffer[i].Tier != tier) continue;
                tierDefinition = tierBuffer[i];
                return true;
            }

            tierDefinition = default;
            return false;
        }

        public static bool TryGetFirstSphereDefinition(DynamicBuffer<SuikaPrefabTierBuffer> tierBuffer, out SuikaPrefabTierBuffer tierDefinition)
        {
            for (int i = 0; i < tierBuffer.Length; i++) {
                if (tierBuffer[i].Shape != SphereShape) continue;
                tierDefinition = tierBuffer[i];
                return true;
            }

            tierDefinition = default;
            return false;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct DropperSpawnSystem : ISystem
    {
        BufferLookup<SuikaPrefabTierBuffer> _tierBufferLookup;
        ComponentLookup<DropperSpawnPoint> _spawnPointLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SuikaGameConfig>();
            state.RequireForUpdate<SuikaPrefabTierBuffer>();
            state.RequireForUpdate<DropperSpawnPoint>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();

            _tierBufferLookup = state.GetBufferLookup<SuikaPrefabTierBuffer>(true);
            _spawnPointLookup = state.GetComponentLookup<DropperSpawnPoint>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // 1. Update lookups and get config.
            _tierBufferLookup.Update(ref state);
            _spawnPointLookup.Update(ref state);
            var config = SystemAPI.GetSingletonRW<SuikaGameConfig>();

            // 2. Collect and count all spawn requests on the main thread.
            var requests = new NativeList<Entity>(Allocator.Temp);
            int totalSpawnCount = 0;
            foreach (var (request, requestEntity) in SystemAPI.Query<DropperInitialSpawnRequest>().WithEntityAccess())
            {
                totalSpawnCount += math.max(0, request.Count);
                requests.Add(requestEntity);
            }

            // 3. Consume all requests on the main thread BEFORE scheduling any jobs.
            foreach (var entity in requests)
            {
                ecb.RemoveComponent<DropperInitialSpawnRequest>(entity);
            }

            // 4. Create single parallel writer for all subsequent jobs.
            var parallelEcb = ecb.AsParallelWriter();

            // 5. Consolidated spawning job if there's work to do.
            if (totalSpawnCount > 0 && 
                SystemAPI.TryGetSingletonEntity<SuikaGameConfig>(out var configEntity) &&
                SystemAPI.TryGetSingletonEntity<DropperSpawnPoint>(out var dropperSpawnEntity))
            {
                var dropperPosition = _spawnPointLookup[dropperSpawnEntity].Position;
                int startSpawnIndex = config.ValueRO.NextSpawnIndex;
                uint seed = config.ValueRO.Seed;

                // Reserve indices on main thread to maintain determinism.
                config.ValueRW.NextSpawnIndex += totalSpawnCount;

                state.Dependency = new DropperSpawnParallelJob
                {
                    ECB = parallelEcb,
                    TierBufferLookup = _tierBufferLookup,
                    ConfigEntity = configEntity,
                    DropperPosition = dropperPosition,
                    Seed = seed,
                    StartSpawnIndex = startSpawnIndex
                }.Schedule(totalSpawnCount, 64, state.Dependency);
            }

            // 6. Handle stress test auto-spawning (chained correctly).
            state.Dependency = new DropperStressTestJob
            {
                DeltaTime = deltaTime,
                ECB = parallelEcb,
                SortKeyOffset = totalSpawnCount
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public partial struct DropperStressTestJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            public int SortKeyOffset;

            public void Execute(Entity entity, [EntityIndexInQuery] int entityInQueryIndex, ref DropperStressTestConfig stressConfig)
            {
                if (stressConfig.Enabled == 0)
                    return;

                stressConfig.TimeSinceLastSpawnSeconds += DeltaTime;
                if (stressConfig.TimeSinceLastSpawnSeconds >= stressConfig.SpawnIntervalSeconds)
                {
                    stressConfig.TimeSinceLastSpawnSeconds = 0f;
                    ECB.AddComponent(SortKeyOffset + entityInQueryIndex, entity, new DropperInitialSpawnRequest
                    {
                        Count = stressConfig.SpawnCountPerInterval
                    });
                }
            }
        }

        [BurstCompile]
        public struct DropperSpawnParallelJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public BufferLookup<SuikaPrefabTierBuffer> TierBufferLookup;
            public Entity ConfigEntity;
            public float3 DropperPosition;
            public uint Seed;
            public int StartSpawnIndex;

            public void Execute(int index)
            {
                int currentSpawnIndex = StartSpawnIndex + index;
                byte tier = DropperSpawnSequenceService.GetTier(Seed, currentSpawnIndex);
                float sphereScale = DropperSpawnSequenceService.GetScale(tier);
                float cylinderScaleXY = 1f;

                if (!TierBufferLookup.TryGetBuffer(ConfigEntity, out var tierBuffer))
                    return;

                if (!DropperSpawnSequenceService.TryGetTierDefinition(tierBuffer, tier, out var tierDefinition))
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
        }

    }
}