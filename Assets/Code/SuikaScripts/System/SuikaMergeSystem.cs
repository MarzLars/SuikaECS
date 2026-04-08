using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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
        public struct MergeEvent
        {
            public Entity EntityA;
            public Entity EntityB;
        }

        [BurstCompile]
        struct MergeCollisionJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentLookup<SuikaItem> ItemLookup;
            public NativeQueue<MergeEvent>.ParallelWriter MergeQueue;

            public void Execute(CollisionEvent collisionEvent)
            {
                if (!ItemLookup.HasComponent(collisionEvent.EntityA) || !ItemLookup.HasComponent(collisionEvent.EntityB))
                    return;

                var itemA = ItemLookup[collisionEvent.EntityA];
                var itemB = ItemLookup[collisionEvent.EntityB];
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
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<DropperSpawnPoint>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            var entityCommandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var config = SystemAPI.GetSingleton<SuikaGameConfig>();
            var itemLookup = SystemAPI.GetComponentLookup<SuikaItem>(true);
            var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

            var mergeQueue = new NativeQueue<MergeEvent>(Allocator.TempJob);
            var collisionJob = new MergeCollisionJob
            {
                ItemLookup = itemLookup,
                MergeQueue = mergeQueue.AsParallelWriter()
            };

            state.Dependency = collisionJob.Schedule(simulationSingleton, state.Dependency);
            state.Dependency.Complete();

            var consumed = new NativeHashSet<Entity>(mergeQueue.Count, Allocator.Temp);

            while (mergeQueue.TryDequeue(out var mergeEvent))
            {
                if (consumed.Contains(mergeEvent.EntityA) || consumed.Contains(mergeEvent.EntityB))
                    continue;

                var first = itemLookup[mergeEvent.EntityA];
                var second = itemLookup[mergeEvent.EntityB];
                if (first.Shape != second.Shape || first.Tier != second.Tier)
                    continue;

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

                var resultPrefab = ResolvePrefabByDefinition(nextShape, nextTier,
                    config);
                if (resultPrefab == Entity.Null)
                    continue;

                consumed.Add(mergeEvent.EntityA);
                consumed.Add(mergeEvent.EntityB);

                var survivorTransform = transformLookup[mergeEvent.EntityA];
                var victimTransform = transformLookup[mergeEvent.EntityB];
                var mergedPosition = (survivorTransform.Position + victimTransform.Position) * 0.5f;

                var mergedEntity = entityCommandBuffer.Instantiate(resultPrefab);
                entityCommandBuffer.SetComponent(mergedEntity, new LocalTransform
                {
                    Position = mergedPosition,
                    Rotation = quaternion.identity,
                    Scale = nextShape == DropperSpawnSequenceService.SphereShape
                        ? DropperSpawnSequenceService.GetScale(nextTier)
                        : 1f
                });
                entityCommandBuffer.AddComponent(mergedEntity, new SuikaItem
                {
                    Tier = nextTier,
                    Shape = nextShape,
                    SpawnIndex = math.min(first.SpawnIndex, second.SpawnIndex)
                });

                entityCommandBuffer.DestroyEntity(mergeEvent.EntityA);
                entityCommandBuffer.DestroyEntity(mergeEvent.EntityB);
            }

            consumed.Dispose();
            mergeQueue.Dispose();
        }


        static Entity ResolvePrefabByDefinition(byte shape,
            byte tier,
            SuikaGameConfig config)
        {
            if (shape == DropperSpawnSequenceService.SphereShape)
            {
                return tier switch
                {
                    2 => config.Sphere2PrefabEntity,
                    1 => config.Sphere1PrefabEntity,
                    _ => config.Sphere0PrefabEntity
                };
            }

            return tier switch
            {
                5 => config.Cylinder5PrefabEntity,
                4 => config.Cylinder4PrefabEntity,
                _ => config.Cylinder3PrefabEntity
            };
        }
    }
}