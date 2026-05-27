using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

namespace SuikaScripts
{
    public struct ClipVelocitiesData : IComponentData
    { }

    public class VelocityClipping : MonoBehaviour
    {
        class VelocityClippingBaker : Baker<VelocityClipping>
        {
            public override void Bake(VelocityClipping authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ClipVelocitiesData>(entity);
            }
        }
    }

    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(ExportPhysicsWorld))]
    [BurstCompile]
    public partial struct VelocityClippingSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<ClipVelocitiesData>();
            state.RequireForUpdate<PhysicsStep>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (!SystemAPI.TryGetSingleton<PhysicsStep>(out var physicsStep))
                return;

            if (physicsStep.SimulationType != SimulationType.UnityPhysics)
                return;

            state.Dependency = new ClipVelocitiesJob {
                GravityFactorLookup = SystemAPI.GetComponentLookup<PhysicsGravityFactor>(true)
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct ClipVelocitiesJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<PhysicsGravityFactor> GravityFactorLookup;

            const float LinearSleepThreshold = 0.01f;
            const float AngularSleepThreshold = 0.01f;

            void Execute(Entity entity, ref PhysicsVelocity velocity) {
                var factor = 1.0f;
                if (GravityFactorLookup.HasComponent(entity)) factor = GravityFactorLookup[entity].Value;

                // Only clip tiny jitter. Do NOT fight gravity integration.
                // Previous heuristic compared against gravity step and froze newly spawned bodies.
                float linearSpeedSq = math.lengthsq(velocity.Linear);
                float angularSpeedSq = math.lengthsq(velocity.Angular);

                float linearThresholdSq = LinearSleepThreshold * LinearSleepThreshold;
                float angularThresholdSq = AngularSleepThreshold * AngularSleepThreshold;

                if (factor <= 0f && linearSpeedSq < linearThresholdSq) velocity.Linear = float3.zero;

                if (angularSpeedSq < angularThresholdSq) velocity.Angular = float3.zero;
            }
        }
    }
}