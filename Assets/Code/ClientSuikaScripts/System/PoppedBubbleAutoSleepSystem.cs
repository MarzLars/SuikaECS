using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace SuikaScripts
{
    /// <summary>
    ///     Simplified system that transitions Burst Spheres to Static mode after a delay.
    ///     This saves significant performance by removing sleeping spheres from the physics simulation.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct PoppedBubbleAutoSleepSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {
            // Require a command buffer that runs at the beginning of the fixed-step so we
            // can apply static transitions before the physics simulation runs.
            state.RequireForUpdate<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var ecbSingleton = SystemAPI.GetSingleton<BeginFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            // Transitioning entities to static by removing velocity/mass components using ECB
            state.Dependency = new PoppedBubbleSleepJob {
                DeltaTime = deltaTime,
                ECB = ecb
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(PhysicsVelocity))] // Only process spheres that are currently dynamic
        partial struct PoppedBubbleSleepJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;

            void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, ref PoppedBubbleSleepTimer timer) {
                if (timer.IsSleeping != 0)
                    return;

                timer.SecondsRemaining -= DeltaTime;
                if (timer.SecondsRemaining > 0f)
                    return;

                timer.IsSleeping = 1;
                timer.SecondsRemaining = 0f;

                // Remove dynamic body components so physics treats this as static this step.
                ECB.RemoveComponent<PhysicsVelocity>(chunkIndex, entity);
                ECB.RemoveComponent<PhysicsMass>(chunkIndex, entity);
                ECB.RemoveComponent<PhysicsDamping>(chunkIndex, entity);
                ECB.RemoveComponent<PhysicsGravityFactor>(chunkIndex, entity);

                // Enable static tag for filtering
                ECB.SetComponentEnabled<PoppedBubbleStaticTag>(chunkIndex, entity, true);
            }
        }
    }
}