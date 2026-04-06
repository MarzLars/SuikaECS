using Code.CodeMonkey_EcsTutorial.RotationAndMovement.Components.Tags;
using Unity.Entities;
using UnityEngine;

namespace Code.CodeMonkey_EcsTutorial.RotationAndMovement.Authoring.Tags
{
    public class RotatingCubeAuthoring : MonoBehaviour
    {
        public class Baker : Baker<RotatingCubeAuthoring>
        {
            public override void Bake(RotatingCubeAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RotatingCube());
            }
        }
    }
}