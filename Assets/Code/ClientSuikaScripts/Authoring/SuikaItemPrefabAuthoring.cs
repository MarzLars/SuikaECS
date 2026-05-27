using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
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
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        public SuikaItemShapeAuthoring shape = SuikaItemShapeAuthoring.Sphere;

        [Range(DropperSpawnSequenceService.MinTier, DropperSpawnSequenceService.CylinderMaxTier)]
        public int tier;

        public Color color = Color.white;
        public Material material; // Material reference for the renderer (optional; uses material color if assigned)
        public bool canMerge = true;
        public bool isBurstPrefab;

        public sealed class Baker : Baker<SuikaItemPrefabAuthoring>
        {
            public override void Bake(SuikaItemPrefabAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var shape = (byte)authoring.shape;

                int clampedTier = shape == DropperSpawnSequenceService.SphereShape ?
                    Mathf.Clamp(authoring.tier, DropperSpawnSequenceService.MinTier,
                        DropperSpawnSequenceService.SphereMaxTier) :
                    Mathf.Clamp(authoring.tier, DropperSpawnSequenceService.CylinderMinTier,
                        DropperSpawnSequenceService.CylinderMaxTier);

                AddComponent(entity, new SuikaItemPrefabDefinition {
                    Shape = shape,
                    Tier = (byte)clampedTier
                });

                AddComponent(entity, new SuikaItem {
                    Tier = (byte)clampedTier,
                    Shape = shape,
                    SpawnIndex = -1,
                    CanMerge = authoring.canMerge ?
                        (byte)1 :
                        (byte)0
                });

                // Add base color: use material color if assigned, otherwise use authoring color.
                // SuikaItemTriggerFlashSystem will flash this to red when item is in trigger zone.
                // Hybrid Renderer auto-propagates SuikaColorOverride via LinkedEntityGroup to child renderers.
                var baseColor = authoring.material ? authoring.material.color : authoring.color;
                var baseColorValue = new float4(baseColor.r, baseColor.g, baseColor.b, baseColor.a);
                var baseEmission = authoring.material && authoring.material.HasProperty(EmissionColor) ?
                    authoring.material.GetColor(EmissionColor) :
                    Color.black;
                var baseEmissionValue = new float4(baseEmission.r, baseEmission.g, baseEmission.b, baseEmission.a);
                AddComponent(entity, new SuikaColorOverride { Value = baseColorValue });
                AddComponent(entity, new URPMaterialPropertyEmissionColor { Value = baseEmissionValue });

                if (!authoring.isBurstPrefab) return;
                AddComponent(entity, new PoppedBubble());

                AddComponent(entity, new PoppedBubbleSleepTimer {
                    SecondsRemaining = 0f,
                    IsSleeping = 0
                });
                SetComponentEnabled<PoppedBubbleSleepTimer>(entity, false);

                AddComponent(entity, new PoppedBubbleStaticTag());
                SetComponentEnabled<PoppedBubbleStaticTag>(entity, false);
            }
        }
    }
}