using Unity.Entities;
using Unity.Transforms;

namespace Unity.DotsUISample
{
    // update after physics so we can get the accurate player position
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial struct QuestSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<GameData>();
            state.RequireForUpdate<UIScreens>();
        }

        public void OnUpdate(ref SystemState state) {
            var game = SystemAPI.GetSingletonRW<GameData>();
            var quest = game.ValueRO.Quest.Value;

            if (game.ValueRO.State != GameState.Questing) return;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var playerEntity = SystemAPI.GetSingletonEntity<Player>();
            var playerPosition = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position.xz;

            const float minDistanceSq = 9f;

            // check for interaction with cauldron to turn in quest
            if (quest.HasAllItems)
                // if close to the cauldron
                if (GameInput.Interact.WasPerformedThisFrame())
                    quest.Done = true;
        }
        // pick up collectables
    }
}