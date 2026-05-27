using Unity.Entities;

namespace SuikaScripts
{
    public struct SuikaItem : IComponentData
    {
        public byte Tier;
        public byte Shape;
        public int SpawnIndex;
        public byte CanMerge;
    }

    public struct SuikaItemPrefabDefinition : IComponentData
    {
        public byte Tier;
        public byte Shape;
    }

    public struct PoppedBubble : IComponentData
    { }

    public struct PoppedBubbleSleepTimer : IComponentData, IEnableableComponent
    {
        public float SecondsRemaining;
        public byte IsSleeping;
    }

    public struct PoppedBubbleStaticTag : IComponentData, IEnableableComponent
    { }
}