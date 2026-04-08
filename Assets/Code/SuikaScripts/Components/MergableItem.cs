using Unity.Entities;

public struct SuikaItem : IComponentData
{
    public byte Tier;
    public byte Shape;
    public int SpawnIndex;
}

public struct SuikaItemPrefabDefinition : IComponentData
{
    public byte Tier;
    public byte Shape;
}
