/* Code Monkey Code example of Aspect use-case, to "fix" the following code:
public partial struct HandleCubeSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach ((
            RefRW<LocalTransform> localTransform,
            RefRO<RotateSpeed> rotateSpeed,
            RefRO<Movement> movement)
            in SystemAPI.Query<RefRW<LocalTransform>, RefRO<RotateSpeed>, RefRO<Movement>>().WithAll<RotatingCube>())
        {
            localTransform.ValueRW = localTransform.ValueRO.RotateY(rotateSpeed.ValueRO.speed * SystemAPI.Time.DeltaTime);
            localTransform.ValueRW = localTransform.ValueRO.Translate(movement.ValueRO.movementVector * SystemAPI.Time.DeltaTime);
        }
    }
}
*/

