using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace SuikaScripts
{
    public struct ClipVelocitiesData : IComponentData
    {
    }

    public class VelocityClipping : MonoBehaviour
    {
        class VelocityClippingBaker : Baker<VelocityClipping>
        {
            public override void Bake(VelocityClipping authoring)
            {
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
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ClipVelocitiesData>();
            state.RequireForUpdate<PhysicsStep>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsStep = SystemAPI.GetSingleton<PhysicsStep>();
            
            if (physicsStep.SimulationType != SimulationType.UnityPhysics)
                return;

            var deltaTime = SystemAPI.Time.DeltaTime;
            var gravity = physicsStep.Gravity;

            state.Dependency = new ClipVelocitiesJob
            {
                TimeStep = deltaTime,
                Gravity = gravity,
                GravityFactorLookup = SystemAPI.GetComponentLookup<PhysicsGravityFactor>(true)
            }.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        partial struct ClipVelocitiesJob : IJobEntity
        {
            public float TimeStep;
            public float3 Gravity;
            [ReadOnly] public ComponentLookup<PhysicsGravityFactor> GravityFactorLookup;

            void Execute(Entity entity, ref PhysicsVelocity velocity, ref LocalTransform transform, in PhysicsMass mass)
            {
                float gravityLengthInOneStep = math.length(Gravity * TimeStep);
                
                float factor = 1.0f;
                if (GravityFactorLookup.HasComponent(entity))
                {
                    factor = GravityFactorLookup[entity].Value;
                }

                // Clip velocities using a simple heuristic:
                // zero out velocities that are smaller than gravity in one step
                if (math.length(velocity.Linear) < factor * gravityLengthInOneStep)
                {
                    // Revert integration (correcting the exported position)
                    transform.Position -= velocity.Linear * TimeStep;

                    // Clip velocity
                    velocity.Linear = float3.zero;
                    velocity.Angular = float3.zero;
                }
            }
        }
    }
}