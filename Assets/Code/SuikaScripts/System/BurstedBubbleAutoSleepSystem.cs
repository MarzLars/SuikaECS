using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace SuikaScripts
{
    /// <summary>
    /// Simplified system that transitions Burst Spheres to Static mode after a delay.
    /// This saves significant performance by removing sleeping spheres from the physics simulation.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct BurstedBubbleAutoSleepSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Transitioning entities to static by removing velocity/mass components.
            state.Dependency = new BurstedBubbleSleepJob
            {
                DeltaTime = deltaTime,
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(PhysicsVelocity))] // Only process spheres that are currently dynamic
        partial struct BurstedBubbleSleepJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, ref BurstedBubbleSleepTimer timer)
            {
                if (timer.IsSleeping != 0)
                    return;

                timer.SecondsRemaining -= DeltaTime;
                if (timer.SecondsRemaining > 0f)
                    return;

                timer.IsSleeping = 1;
                timer.SecondsRemaining = 0f;

                // Remove ALL components that make it a dynamic/kinematic body.
                // In Unity Physics, a static body is one with a PhysicsCollider but NO PhysicsVelocity or PhysicsMass.
                ECB.RemoveComponent<PhysicsVelocity>(chunkIndex, entity);
                ECB.RemoveComponent<PhysicsMass>(chunkIndex, entity);
                ECB.RemoveComponent<PhysicsDamping>(chunkIndex, entity);
                ECB.RemoveComponent<PhysicsGravityFactor>(chunkIndex, entity);

                // Enable the static tag for other systems to filter by.
                ECB.SetComponentEnabled<BurstedBubbleStaticTag>(chunkIndex, entity, true);
            }
        }
    }
}
