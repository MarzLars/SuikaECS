using Code.InputHandling;
using Unity.Entities;
using Unity.Mathematics;

namespace SuikaScripts
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(DropperSpawnSystem))]
    public partial struct DropperInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SuikaGameConfig>();
            state.RequireForUpdate<DropperSpawnPoint>();
            state.RequireForUpdate<DropperAimConfig>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state) {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            float moveX = GameInput.GetMoveX();
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (spawnPoint, aimConfig) in
                     SystemAPI.Query<RefRW<DropperSpawnPoint>, RefRO<DropperAimConfig>>())
                if (math.abs(moveX) > 0.001f) {
                    float nextX = spawnPoint.ValueRO.Position.x + moveX * aimConfig.ValueRO.MoveSpeed * deltaTime;
                    spawnPoint.ValueRW.Position.x = math.clamp(nextX, aimConfig.ValueRO.MinX, aimConfig.ValueRO.MaxX);
                }

            if (!GameInput.DropPressedThisFrame())
                return;

            var configEntity = SystemAPI.GetSingletonEntity<SuikaGameConfig>();
            if (SystemAPI.HasComponent<DropperInitialSpawnRequest>(configEntity))
                return;

            if (SystemAPI.HasComponent<DropperSpawnBlocked>(configEntity))
                return;

            ecb.AddComponent(configEntity, new DropperInitialSpawnRequest {
                Count = 1
            });
            SystemAPI.SetComponentEnabled<DropperPreviewReady>(configEntity, false);

            // Immediately destroy current preview entity so no visual residue remains during drop action.
            if (SystemAPI.TryGetSingletonEntity<DropperSpawnPoint>(out var spawnPointEntity))
                if (SystemAPI.HasComponent<DropperPreviewEntity>(spawnPointEntity)) {
                    var previewRef = SystemAPI.GetComponent<DropperPreviewEntity>(spawnPointEntity);
                    if (previewRef.Value != Entity.Null) {
                        ecb.DestroyEntity(previewRef.Value);
                        ecb.SetComponent(spawnPointEntity, new DropperPreviewEntity { Value = Entity.Null });
                    }
                }
        }
    }
}