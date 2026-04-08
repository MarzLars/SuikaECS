using SuikaScripts;
using Unity.Entities;
using UnityEngine.InputSystem;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(DropperSpawnSystem))]
public partial struct DropperInputSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SuikaGameConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        bool clicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool pressed = Keyboard.current != null && 
                       (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame);

        if (!clicked && !pressed)
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