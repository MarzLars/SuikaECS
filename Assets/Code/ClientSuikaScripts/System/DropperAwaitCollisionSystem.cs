using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace SuikaScripts
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct DropperAwaitCollisionSystem : ISystem
    {
        [BurstCompile]
        struct CollisionWatchJob : ICollisionEventsJob
        {
            [ReadOnly] public ComponentLookup<DroppedAwaitCollision> AwaitLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            public void Execute(CollisionEvent collisionEvent) {
                // If either body is a dropped item awaiting collision, record it
                if (AwaitLookup.HasComponent(collisionEvent.EntityA)) {
                    var awaitComp = AwaitLookup[collisionEvent.EntityA];
                    ECB.RemoveComponent<DroppedAwaitCollision>(collisionEvent.BodyIndexA, collisionEvent.EntityA);
                    ECB.RemoveComponent<DropperSpawnBlocked>(collisionEvent.BodyIndexA, awaitComp.OwnerConfigEntity);
                    ECB.SetComponentEnabled<DropperPreviewReady>(collisionEvent.BodyIndexA, awaitComp.OwnerConfigEntity,
                        true);
                }

                if (!AwaitLookup.HasComponent(collisionEvent.EntityB)) return;
                {
                    var awaitComp = AwaitLookup[collisionEvent.EntityB];
                    ECB.RemoveComponent<DroppedAwaitCollision>(collisionEvent.BodyIndexB, collisionEvent.EntityB);
                    ECB.RemoveComponent<DropperSpawnBlocked>(collisionEvent.BodyIndexB, awaitComp.OwnerConfigEntity);
                    ECB.SetComponentEnabled<DropperPreviewReady>(collisionEvent.BodyIndexB, awaitComp.OwnerConfigEntity,
                        true);
                }
            }
        }

        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (!SystemAPI.TryGetSingleton<SimulationSingleton>(out var simulationSingleton))
                return;
            var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged)
                .AsParallelWriter();

            state.Dependency = new CollisionWatchJob {
                AwaitLookup = SystemAPI.GetComponentLookup<DroppedAwaitCollision>(true),
                ECB = ecb
            }.Schedule(simulationSingleton, state.Dependency);
        }
    }
}