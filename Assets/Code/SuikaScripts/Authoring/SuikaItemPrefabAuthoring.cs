using Unity.Entities;
using UnityEngine;

namespace SuikaScripts
{
    public enum SuikaItemShapeAuthoring : byte
    {
        Sphere = DropperSpawnSequenceService.SphereShape,
        Cylinder = DropperSpawnSequenceService.CylinderShape
    }

    [DisallowMultipleComponent]
    public sealed class SuikaItemPrefabAuthoring : MonoBehaviour
    {
        public SuikaItemShapeAuthoring shape = SuikaItemShapeAuthoring.Sphere;

        [Range(DropperSpawnSequenceService.MinTier, DropperSpawnSequenceService.CylinderMaxTier)]
        public int tier;

        public Color color = Color.white;
        public bool canMerge = true;
        public bool isBurstPrefab = false;

        public sealed class Baker : Baker<SuikaItemPrefabAuthoring>
        {
            public override void Bake(SuikaItemPrefabAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                byte shape = (byte)authoring.shape;

                int clampedTier = shape == DropperSpawnSequenceService.SphereShape
                    ? Mathf.Clamp(authoring.tier, DropperSpawnSequenceService.MinTier, DropperSpawnSequenceService.SphereMaxTier)
                    : Mathf.Clamp(authoring.tier, DropperSpawnSequenceService.CylinderMinTier, DropperSpawnSequenceService.CylinderMaxTier);

                AddComponent(entity, new SuikaItemPrefabDefinition
                {
                    Shape = shape,
                    Tier = (byte)clampedTier
                });

                AddComponent(entity, new SuikaItem
                {
                    Tier = (byte)clampedTier,
                    Shape = shape,
                    SpawnIndex = -1,
                    CanMerge = authoring.canMerge ? (byte)1 : (byte)0
                });

                AddComponent(entity, new SuikaColorOverride
                {
                    Value = new Unity.Mathematics.float4(authoring.color.r, authoring.color.g, authoring.color.b, authoring.color.a)
                });

                if (authoring.isBurstPrefab)
                {
                    AddComponent(entity, new BurstedBubble());

                    AddComponent(entity, new BurstedBubbleSleepTimer
                    {
                        SecondsRemaining = 0f,
                        IsSleeping = 0
                    });
                    SetComponentEnabled<BurstedBubbleSleepTimer>(entity, false);

                    AddComponent(entity, new BurstedBubbleStaticTag());
                    SetComponentEnabled<BurstedBubbleStaticTag>(entity, false);
                }
            }
        }
    }
}
