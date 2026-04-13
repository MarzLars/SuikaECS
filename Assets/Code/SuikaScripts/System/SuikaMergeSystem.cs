using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace SuikaScripts
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct SuikaMergeSystem : ISystem
    {
        ComponentLookup<SuikaItem> _itemLookup;
        ComponentLookup<BurstedBubbleStaticTag> _staticTagLookup;
        ComponentLookup<LocalTransform> _transformLookup;
        ComponentLookup<PhysicsCollider> _colliderLookup;
        ComponentLookup<Suika.UI.SuikaScore> _scoreLookup;
        BufferLookup<SuikaPrefabTierBuffer> _tierBufferLookup;
        BufferLookup<SuikaBurstConfigBuffer> _burstBufferLookup;

        public struct MergeEvent
        {
            public Entity EntityA;
            public Entity EntityB;
        }

        [BurstCompile]
        struct MergeCollisionJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentLookup<SuikaItem> ItemLookup;
            [ReadOnly] public ComponentLookup<BurstedBubbleStaticTag> StaticTagLookup;
            public NativeQueue<MergeEvent>.ParallelWriter MergeQueue;

            public void Execute(CollisionEvent collisionEvent)
            {
                if (!ItemLookup.HasComponent(collisionEvent.EntityA) || !ItemLookup.HasComponent(collisionEvent.EntityB))
                    return;

                if (StaticTagLookup.HasComponent(collisionEvent.EntityA) && StaticTagLookup.IsComponentEnabled(collisionEvent.EntityA))
                    return;

                if (StaticTagLookup.HasComponent(collisionEvent.EntityB) && StaticTagLookup.IsComponentEnabled(collisionEvent.EntityB))
                    return;

                var itemA = ItemLookup[collisionEvent.EntityA];
                var itemB = ItemLookup[collisionEvent.EntityB];
                if (itemA.CanMerge == 0 || itemB.CanMerge == 0)
                    return;

                if (itemA.Shape != itemB.Shape || itemA.Tier != itemB.Tier)
                    return;

                MergeQueue.Enqueue(new MergeEvent
                {
                    EntityA = collisionEvent.EntityA,
                    EntityB = collisionEvent.EntityB
                });
            }
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SuikaGameConfig>();
            state.RequireForUpdate<Suika.UI.SuikaScore>();
            state.RequireForUpdate<SuikaPrefabTierBuffer>();
            state.RequireForUpdate<SuikaBurstConfigBuffer>();
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<DropperSpawnPoint>();

            _itemLookup = state.GetComponentLookup<SuikaItem>(isReadOnly: false);
            _staticTagLookup = state.GetComponentLookup<BurstedBubbleStaticTag>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _colliderLookup = state.GetComponentLookup<PhysicsCollider>(true);
            _scoreLookup = state.GetComponentLookup<Suika.UI.SuikaScore>(isReadOnly: false);
            _tierBufferLookup = state.GetBufferLookup<SuikaPrefabTierBuffer>(true);
            _burstBufferLookup = state.GetBufferLookup<SuikaBurstConfigBuffer>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<SimulationSingleton>(out var simulationSingleton))
                return;

            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var entityCommandBuffer = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            if (!SystemAPI.TryGetSingletonEntity<SuikaGameConfig>(out var configEntity))
                return;
            
            _itemLookup.Update(ref state);
            _staticTagLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _colliderLookup.Update(ref state);
            _scoreLookup.Update(ref state);
            _tierBufferLookup.Update(ref state);
            _burstBufferLookup.Update(ref state);

            var mergeQueue = new NativeQueue<MergeEvent>(Allocator.TempJob);
            var collisionJob = new MergeCollisionJob
            {
                ItemLookup = _itemLookup,
                StaticTagLookup = _staticTagLookup,
                MergeQueue = mergeQueue.AsParallelWriter()
            };

            state.Dependency = collisionJob.Schedule(simulationSingleton, state.Dependency);

            var processMergeJob = new ProcessMergeJob
            {
                ECB = entityCommandBuffer.AsParallelWriter(),
                MergeQueue = mergeQueue,
                ItemLookup = _itemLookup,
                TransformLookup = _transformLookup,
                ScoreLookup = _scoreLookup,
                TierBufferLookup = _tierBufferLookup,
                BurstBufferLookup = _burstBufferLookup,
                ConfigEntity = configEntity
            };

            state.Dependency = processMergeJob.Schedule(state.Dependency);
            state.Dependency = mergeQueue.Dispose(state.Dependency);
        }

        [BurstCompile]
        public struct ProcessMergeJob : IJob
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public NativeQueue<MergeEvent> MergeQueue;
            public ComponentLookup<SuikaItem> ItemLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            public ComponentLookup<Suika.UI.SuikaScore> ScoreLookup;
            [ReadOnly] public BufferLookup<SuikaPrefabTierBuffer> TierBufferLookup;
            [ReadOnly] public BufferLookup<SuikaBurstConfigBuffer> BurstBufferLookup;
            public Entity ConfigEntity;

            public void Execute()
            {
                if (!MergeQueue.IsCreated || MergeQueue.IsEmpty())
                    return;

                if (!TierBufferLookup.HasBuffer(ConfigEntity) || !BurstBufferLookup.HasBuffer(ConfigEntity))
                    return;

                var tierBuffer = TierBufferLookup[ConfigEntity];
                var burstBuffer = BurstBufferLookup[ConfigEntity];
                var consumed = new NativeHashSet<Entity>(math.max(8, MergeQueue.Count), Allocator.Temp);

                while (MergeQueue.TryDequeue(out var mergeEvent))
                {
                    if (consumed.Contains(mergeEvent.EntityA) || consumed.Contains(mergeEvent.EntityB))
                        continue;

                    if (!ItemLookup.HasComponent(mergeEvent.EntityA) || !ItemLookup.HasComponent(mergeEvent.EntityB))
                        continue;

                    if (!TransformLookup.HasComponent(mergeEvent.EntityA) || !TransformLookup.HasComponent(mergeEvent.EntityB))
                        continue;

                    var first = ItemLookup[mergeEvent.EntityA];
                    var second = ItemLookup[mergeEvent.EntityB];
                    if (first.CanMerge == 0 || second.CanMerge == 0)
                        continue;

                    if (first.Shape != second.Shape || first.Tier != second.Tier)
                        continue;

                    var survivorTransform = TransformLookup[mergeEvent.EntityA];
                    var victimTransform = TransformLookup[mergeEvent.EntityB];
                    var mergedPosition = (survivorTransform.Position + victimTransform.Position) * 0.5f;

                    if (!TryGetTierDefinition(tierBuffer, first.Tier, out var currentTierDefinition))
                        continue;

                    if (currentTierDefinition.Shape != first.Shape)
                        continue;

                    bool shouldBurst = currentTierDefinition.BurstOnMerge != 0;

                    if (shouldBurst)
                    {
                        if (ScoreLookup.HasComponent(ConfigEntity))
                        {
                            var score = ScoreLookup[ConfigEntity];
                            score.Value += currentTierDefinition.ScoreValue;
                            ScoreLookup[ConfigEntity] = score;
                        }

                        if (!TryGetBurstConfig(burstBuffer, currentTierDefinition.BurstConfigIndex, out var selectedBurstConfig))
                            continue;

                        if (!TrySpawnMaxTierBurst(
                                ECB,
                                tierBuffer,
                                TransformLookup,
                                selectedBurstConfig,
                                mergedPosition,
                                first,
                                second,
                                currentTierDefinition))
                            continue;

                        consumed.Add(mergeEvent.EntityA);
                        consumed.Add(mergeEvent.EntityB);
                        
                        // Immediate update to prevent re-merging in subsequent fixed steps before destruction
                        first.CanMerge = 0;
                        second.CanMerge = 0;
                        ItemLookup[mergeEvent.EntityA] = first;
                        ItemLookup[mergeEvent.EntityB] = second;
                        
                        ECB.DestroyEntity(0, mergeEvent.EntityA);
                        ECB.DestroyEntity(0, mergeEvent.EntityB);
                        continue;
                    }

                    byte nextShape = first.Shape;
                    byte nextTier;
                    if (first.Shape == DropperSpawnSequenceService.SphereShape)
                    {
                        if (first.Tier >= DropperSpawnSequenceService.SphereMaxTier)
                        {
                            nextShape = DropperSpawnSequenceService.CylinderShape;
                            nextTier = DropperSpawnSequenceService.CylinderMinTier;
                        }
                        else
                        {
                            nextTier = DropperSpawnSequenceService.PromoteTier(first.Tier, DropperSpawnSequenceService.SphereMaxTier);
                        }
                    }
                    else
                    {
                        if (first.Tier >= DropperSpawnSequenceService.CylinderMaxTier)
                            continue;

                        nextTier = DropperSpawnSequenceService.PromoteTier(first.Tier, DropperSpawnSequenceService.CylinderMaxTier);
                    }

                    if (!TryGetTierDefinition(tierBuffer, nextTier, out var nextTierDefinition))
                        continue;

                    if (nextTierDefinition.Shape != nextShape)
                        continue;

                    if (ScoreLookup.HasComponent(ConfigEntity))
                    {
                        var score = ScoreLookup[ConfigEntity];
                        score.Value += nextTierDefinition.ScoreValue;
                        ScoreLookup[ConfigEntity] = score;
                    }

                    var resultPrefab = nextTierDefinition.PrefabEntity;
                    if (resultPrefab == Entity.Null)
                        continue;

                    float sphereScale = DropperSpawnSequenceService.GetScale(nextTier);
                    float cylinderScaleXY = 1f;
                    if (nextTierDefinition.Shape == DropperSpawnSequenceService.SphereShape)
                    {
                        sphereScale = nextTierDefinition.Scale;
                    }
                    else
                    {
                        cylinderScaleXY = nextTierDefinition.Scale;
                    }

                    consumed.Add(mergeEvent.EntityA);
                    consumed.Add(mergeEvent.EntityB);

                    // Immediate update to prevent re-merging in subsequent fixed steps before destruction
                    first.CanMerge = 0;
                    second.CanMerge = 0;
                    ItemLookup[mergeEvent.EntityA] = first;
                    ItemLookup[mergeEvent.EntityB] = second;

                    var mergedEntity = ECB.Instantiate(0, resultPrefab);
                    
                    // Baked components (SuikaItem, SuikaColorOverride) are already present on the prefab.
                    // We only need to update the instance-specific data.

                    ECB.SetComponent(0, mergedEntity, new SuikaColorOverride { Value = nextTierDefinition.Color });
                    ECB.SetComponent(0, mergedEntity, new LocalTransform
                    {
                        Position = mergedPosition,
                        Rotation = quaternion.identity,
                        Scale = nextShape == DropperSpawnSequenceService.SphereShape
                            ? sphereScale
                            : 1f
                    });

                    ECB.RemoveComponent<PostTransformMatrix>(0, mergedEntity);
                    if (nextShape == DropperSpawnSequenceService.CylinderShape)
                    {
                        // Cylinders: scale XY only, keep Z unchanged.
                        ECB.AddComponent(0, mergedEntity, new PostTransformMatrix
                        {
                            Value = float4x4.Scale(cylinderScaleXY, cylinderScaleXY, 1f)
                        });
                    }

                    ECB.SetComponent(0, mergedEntity, new SuikaItem
                    {
                        Tier = nextTier,
                        Shape = nextShape,
                        SpawnIndex = math.min(first.SpawnIndex, second.SpawnIndex),
                        CanMerge = 1
                    });

                    ECB.DestroyEntity(0, mergeEvent.EntityA);
                    ECB.DestroyEntity(0, mergeEvent.EntityB);
                }

                consumed.Dispose();
            }

            static bool TrySpawnMaxTierBurst(
                EntityCommandBuffer.ParallelWriter entityCommandBuffer,
                DynamicBuffer<SuikaPrefabTierBuffer> tierBuffer,
                ComponentLookup<LocalTransform> transformLookup,
                SuikaBurstConfigBuffer burstConfig,
                float3 center,
                SuikaItem first,
                SuikaItem second,
                SuikaPrefabTierBuffer currentTierDefinition)
            {
                int burstCount = math.max(0, burstConfig.SphereCount);
                if (burstCount == 0)
                    return true;

                Entity burstPrefab = burstConfig.SpherePrefabEntity != Entity.Null
                    ? burstConfig.SpherePrefabEntity
                    : currentTierDefinition.PrefabEntity;

                byte burstTier = currentTierDefinition.Tier;
                float burstScale = currentTierDefinition.Scale;
                float4 burstColor = currentTierDefinition.Color;

                if ((burstPrefab == Entity.Null || currentTierDefinition.Shape != DropperSpawnSequenceService.SphereShape) &&
                    TryGetFirstSphereDefinition(tierBuffer, out var sphereTierDefinition))
                {
                    if (burstPrefab == Entity.Null)
                        burstPrefab = sphereTierDefinition.PrefabEntity;

                    burstTier = sphereTierDefinition.Tier;
                    burstScale = sphereTierDefinition.Scale;
                    burstColor = sphereTierDefinition.Color;
                }

                if (burstPrefab == Entity.Null)
                    return false;

                if (transformLookup.HasComponent(burstPrefab))
                    burstScale = transformLookup[burstPrefab].Scale;

                if (burstConfig.SpherePrefabEntity != Entity.Null)
                    burstColor = new float4(1, 1, 1, 1);

                burstScale *= burstConfig.SphereSize;

                float radius = math.max(0f, burstConfig.Radius);
                int baseSpawnIndex = math.min(first.SpawnIndex, second.SpawnIndex);
                float angleStep = (2f * math.PI) / math.max(1, burstCount);

                for (int i = 0; i < burstCount; i++)
                {
                    float angle = angleStep * i;
                    float2 offset = radius * new float2(math.cos(angle), math.sin(angle));
                    float3 spawnPosition = center + new float3(offset.x, offset.y, 0f);

                    var burstEntity = entityCommandBuffer.Instantiate(0, burstPrefab);
                    entityCommandBuffer.SetComponent(0, burstEntity, new SuikaColorOverride
                    {
                        Value = burstColor
                    });
                    
                    entityCommandBuffer.SetComponent(0, burstEntity, LocalTransform.FromPositionRotationScale(
                        spawnPosition,
                        quaternion.identity,
                        burstScale));
                    entityCommandBuffer.RemoveComponent<PostTransformMatrix>(0, burstEntity);
                    entityCommandBuffer.SetComponent(0, burstEntity, new SuikaItem
                    {
                        Tier = burstTier,
                        Shape = DropperSpawnSequenceService.SphereShape,
                        SpawnIndex = baseSpawnIndex + i,
                        CanMerge = 0
                    });
                    entityCommandBuffer.SetComponent(0, burstEntity, new BurstedBubbleSleepTimer
                    {
                        SecondsRemaining = burstConfig.SleepDelaySeconds,
                        IsSleeping = 0
                    });
                    entityCommandBuffer.SetComponentEnabled<BurstedBubbleSleepTimer>(0, burstEntity, true);

                    entityCommandBuffer.SetComponent(0, burstEntity, new BurstedBubbleStaticTag());
                    entityCommandBuffer.SetComponentEnabled<BurstedBubbleStaticTag>(0, burstEntity, false);
                }

                return true;
            }

            static bool TryGetBurstConfig(DynamicBuffer<SuikaBurstConfigBuffer> burstBuffer, int index, out SuikaBurstConfigBuffer burstConfig)
            {
                if (index < 0 || index >= burstBuffer.Length)
                {
                    burstConfig = default;
                    return false;
                }

                burstConfig = burstBuffer[index];
                return true;
            }

            static bool TryGetFirstSphereDefinition(DynamicBuffer<SuikaPrefabTierBuffer> tierBuffer, out SuikaPrefabTierBuffer tierDefinition)
            {
                for (int i = 0; i < tierBuffer.Length; i++)
                {
                    if (tierBuffer[i].Shape == DropperSpawnSequenceService.SphereShape)
                    {
                        tierDefinition = tierBuffer[i];
                        return true;
                    }
                }

                tierDefinition = default;
                return false;
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
