using Code.CodeMonkey_EcsTutorial.RotationAndMovement.Components;
using Code.CodeMonkey_EcsTutorial.RotationAndMovement.Components.Tags;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace Code.CodeMonkey_EcsTutorial.RotationAndMovement.Systems
{
    public partial struct RotatingCubeSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<RotateSpeed>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            //Example of rotation without using Job system:
            /*
        foreach ((RefRW<LocalTransform> localTransform, RefRO<RotateSpeed> rotateSpeed)
            in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotateSpeed>>()){

            localTransform.ValueRW = localTransform.ValueRO.RotateY(rotateSpeed.ValueRO.speed * SystemAPI.Time.DeltaTime);
        }
        */
            //Example using Job System:
            var rotatingCubeJob = new RotateCubeSystemJob {
                deltaTime = SystemAPI.Time.DeltaTime
            };
            rotatingCubeJob.Schedule();
        }

        [BurstCompile]
        [WithNone(typeof(Player))]
        public partial struct RotateCubeSystemJob : IJobEntity
        {
            public float deltaTime;

            void Execute(ref LocalTransform localTransform, in RotateSpeed rotateSpeed) {
                localTransform = localTransform.RotateY(rotateSpeed.speed * deltaTime);
            }
        }
    }
}