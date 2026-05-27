using Suika.UI;
using Unity.Burst;
using Unity.Entities;

namespace SuikaScripts
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [BurstCompile]
    public partial struct GameOverEventSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<BeginPresentationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            var ecb = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var restartRequested = false;

            foreach (var (_, entity) in SystemAPI.Query<RestartClickEvent>().WithEntityAccess()) {
                ecb.DestroyEntity(entity);
                restartRequested = true;
            }

            if (!restartRequested)
                return;

            // Create a RestartRequest marker entity. A managed MonoBehaviour will consume this
            // request and perform the SceneManager.LoadScene call on the main thread.
            var reqEntity = ecb.CreateEntity();
            ecb.AddComponent(reqEntity, new RestartRequest());
        }
    }
}