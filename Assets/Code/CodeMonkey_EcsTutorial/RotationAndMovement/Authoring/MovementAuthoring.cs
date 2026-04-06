using Code.CodeMonkey_EcsTutorial.RotationAndMovement.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.CodeMonkey_EcsTutorial.RotationAndMovement.Authoring
{
    public class MovementAuthoring : MonoBehaviour
    {
        public class Baker : Baker<MovementAuthoring>
        {
            public override void Bake(MovementAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Movement {
                    movementVector = new float3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f))
                });
            }
        }
    }
}