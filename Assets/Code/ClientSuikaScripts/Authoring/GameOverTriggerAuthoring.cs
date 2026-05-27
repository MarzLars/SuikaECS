using Unity.Entities;
using UnityEngine;

namespace SuikaScripts
{
    [DisallowMultipleComponent]
    public sealed class GameOverTriggerAuthoring : MonoBehaviour
    {
        [Min(0.1f)] public float WarningDurationSeconds = 5f;
        [Min(0.1f)] public float FlashFrequencyHz = 3f;
        public float BottomSegmentOffsetY;

        public sealed class Baker : Baker<GameOverTriggerAuthoring>
        {
            public override void Bake(GameOverTriggerAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SuikaGameOverTrigger {
                    WarningDurationSeconds = authoring.WarningDurationSeconds,
                    FlashFrequencyHz = authoring.FlashFrequencyHz,
                    BottomSegmentOffsetY = authoring.BottomSegmentOffsetY
                });
            }
        }
    }
}