using Unity.Entities;
using Unity.Mathematics;

namespace Code.CodeMonkey_EcsTutorial.RotationAndMovement.Components
{
    public struct Movement : IComponentData
    {
        public float3 movementVector;
    }
}