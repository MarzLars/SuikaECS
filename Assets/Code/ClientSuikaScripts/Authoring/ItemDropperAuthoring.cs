using Unity.Entities;
using UnityEngine;

namespace SuikaScripts
{
    [DisallowMultipleComponent]
    public sealed class ItemDropperAuthoring : MonoBehaviour
    {
        [Header("Aiming")] public float AimMoveSpeed = 8f;

        public float AimMinX = -6f;
        public float AimMaxX = 6f;

        [Header("Stress Test")] public bool StressTestEnabled;

        public float StressTestSpawnIntervalSeconds = 1f;
        public int StressTestSpawnCountPerInterval = 1;

        [Header("Aim Ray Visual")] public float AimRayMaxDistance = 25f;

        public LayerMask AimRayLayerMask = ~0;

        public class Baker : Baker<ItemDropperAuthoring>
        {
            public override void Bake(ItemDropperAuthoring dropperAuthoring) {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DropperSpawnPoint {
                    Position = dropperAuthoring.transform.position
                });
                AddComponent(entity, new DropperAimConfig {
                    MoveSpeed = dropperAuthoring.AimMoveSpeed,
                    MinX = dropperAuthoring.AimMinX,
                    MaxX = dropperAuthoring.AimMaxX
                });
                AddComponent(entity, new DropperStressTestConfig {
                    Enabled = dropperAuthoring.StressTestEnabled ? (byte)1 : (byte)0,
                    SpawnIntervalSeconds = dropperAuthoring.StressTestSpawnIntervalSeconds,
                    TimeSinceLastSpawnSeconds = 0f,
                    SpawnCountPerInterval = dropperAuthoring.StressTestSpawnCountPerInterval
                });
                AddComponent(entity, new DropperRaycastVisualConfig {
                    MaxDistance = dropperAuthoring.AimRayMaxDistance,
                    LayerMask = dropperAuthoring.AimRayLayerMask.value
                });
                AddComponent(entity, new DropperPreviewEntity {
                    Value = Entity.Null
                });
            }
        }
    }
}