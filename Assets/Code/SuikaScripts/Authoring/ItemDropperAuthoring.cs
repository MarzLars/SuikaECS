using Unity.Entities;
using UnityEngine;

namespace SuikaScripts
{
    [DisallowMultipleComponent]
    public sealed class ItemDropperAuthoring : MonoBehaviour
    {
        public class Baker : Baker<ItemDropperAuthoring>
        {
            public override void Bake(ItemDropperAuthoring dropperAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DropperSpawnPoint
                {
                    Position = dropperAuthoring.transform.position
                });
            }
        }
    }
}