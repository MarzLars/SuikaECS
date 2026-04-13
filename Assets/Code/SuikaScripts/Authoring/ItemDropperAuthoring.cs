using Unity.Entities;
using UnityEngine;

namespace SuikaScripts
{
    [DisallowMultipleComponent]
    public sealed class ItemDropperAuthoring : MonoBehaviour
    {
        [Header("Stress Test")]
        public bool StressTestEnabled;
        public float StressTestSpawnIntervalSeconds = 1f;
        public int StressTestSpawnCountPerInterval = 1;

        public class Baker : Baker<ItemDropperAuthoring>
        {
            public override void Bake(ItemDropperAuthoring dropperAuthoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new DropperSpawnPoint
                {
                    Position = dropperAuthoring.transform.position
                });
                AddComponent(entity, new DropperStressTestConfig
                {
                    Enabled = dropperAuthoring.StressTestEnabled ? (byte)1 : (byte)0,
                    SpawnIntervalSeconds = dropperAuthoring.StressTestSpawnIntervalSeconds,
                    TimeSinceLastSpawnSeconds = 0f,
                    SpawnCountPerInterval = dropperAuthoring.StressTestSpawnCountPerInterval
                });
            }
        }
    }
}