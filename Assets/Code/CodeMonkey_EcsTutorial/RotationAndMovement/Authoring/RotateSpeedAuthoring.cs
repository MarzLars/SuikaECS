using Code.CodeMonkey_EcsTutorial.RotationAndMovement.Components;
using Unity.Entities;
using UnityEngine;

namespace Code.CodeMonkey_EcsTutorial.RotationAndMovement.Authoring
{
    public class RotateSpeedAuthoring : MonoBehaviour
    {
        public float value;

        class Baker : Baker<RotateSpeedAuthoring>
        {
            public override void Bake(RotateSpeedAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new RotateSpeed { speed = authoring.value });
            }
        }
    }
}