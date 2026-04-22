using Unity.Entities;
using Code.InputHandling;

namespace SuikaScripts
{
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(DropperSpawnSystem))]
public partial struct DropperInputSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SuikaGameConfig>();
        GameInput.Initialize();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!GameInput.DropPressedThisFrame())
            return;

        var configEntity = SystemAPI.GetSingletonEntity<SuikaGameConfig>();
        if (state.EntityManager.HasComponent<DropperInitialSpawnRequest>(configEntity))
            return;
        state.EntityManager.AddComponentData(configEntity, new DropperInitialSpawnRequest
        {
            Count = 1
        });
    }
}
}