using Unity.Entities;
using Unity.Mathematics;

namespace SuikaScripts
{
    /// <summary>
    ///     Marker/settings for trigger zone that starts 5 second game-over countdown.
    ///     Attach to trigger entity in MainEnvironmentSubScene.
    /// </summary>
    public struct SuikaGameOverTrigger : IComponentData
    {
        public float WarningDurationSeconds;
        public float FlashFrequencyHz;
        public float BottomSegmentOffsetY;
    }

    /// <summary>
    ///     Active countdown state on item currently inside loss zone.
    /// </summary>
    public struct SuikaGameOverWarning : IComponentData
    {
        public float SecondsRemaining;
        public float FlashElapsedSeconds;
        public float4 DefaultColor;
        public float4 DefaultEmission;
    }
}