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
            }
        }
    }
}