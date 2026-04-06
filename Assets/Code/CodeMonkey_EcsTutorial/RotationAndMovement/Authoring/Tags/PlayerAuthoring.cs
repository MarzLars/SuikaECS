using Code.CodeMonkey_EcsTutorial.RotationAndMovement.Components.Tags;
using Unity.Entities;
using UnityEngine;

namespace Code.CodeMonkey_EcsTutorial.RotationAndMovement.Authoring.Tags
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Player());
            }
        }
    }
}