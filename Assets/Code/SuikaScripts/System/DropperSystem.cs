using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SuikaScripts
{
    public static class DropperSpawnSequenceService
    {
        public const uint DefaultSeed = 123456789u;
        public const byte MinTier = 0;
        public const byte SphereShape = 0;
        public const byte CylinderShape = 1;
        public const byte SphereMaxTier = 2;
        public const byte CylinderMinTier = 3;
        public const byte CylinderMaxTier = 5;
        const uint SeedWeight = 31u;
        const uint SpawnIndexWeight = 17u;

        public static byte GetTier(uint seed, int spawnIndex)
        {
            uint safeSeed = seed == 0 ? 
                DefaultSeed :
                seed;
            uint safeSpawnIndex = (uint)math.max(spawnIndex, 0);
            uint range = SphereMaxTier + 1;

            uint mixed = safeSeed * SeedWeight + safeSpawnIndex * SpawnIndexWeight;
            return (byte)(MinTier + (mixed % range));
        }

        public static byte PromoteTier(byte tier, byte maxTier)
        {
            return (byte)math.min(tier + 1, maxTier);
        }

        public static float GetScale(byte tier)
        {
            return tier switch
            {
                2 => 0.75f,
                1 => 0.50f,
                _ => 0.25f
            };
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct DropperSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SuikaGameConfig>();
            state.RequireForUpdate<DropperSpawnPoint>();
            state.RequireForUpdate<DropperInitialSpawnRequest>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var requestEntity = SystemAPI.GetSingletonEntity<DropperInitialSpawnRequest>();
            var request = SystemAPI.GetComponent<DropperInitialSpawnRequest>(requestEntity);
            int spawnCount = math.max(0, request.Count);

            if (spawnCount == 0)
            {
                state.EntityManager.RemoveComponent<DropperInitialSpawnRequest>(requestEntity);
                return;
            }

            var config = SystemAPI.GetSingletonRW<SuikaGameConfig>();
            var dropperEntity = SystemAPI.GetSingletonEntity<DropperSpawnPoint>();
            var dropperPosition = SystemAPI.GetComponent<DropperSpawnPoint>(dropperEntity).Position;

            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            int nextSpawnIndex = config.ValueRO.NextSpawnIndex;
            for (int i = 0; i < spawnCount; i++)
            {
                byte tier = DropperSpawnSequenceService.GetTier(config.ValueRO.Seed, nextSpawnIndex);
                Entity prefab = ResolveSpherePrefabByDefinition(config.ValueRO, tier);
                nextSpawnIndex++;

                if (prefab == Entity.Null)
                    continue;

                var spawnedItem = ecb.Instantiate(prefab);
                ecb.SetComponent(spawnedItem, LocalTransform.FromPositionRotationScale(
                    dropperPosition,
                    quaternion.identity,
                    DropperSpawnSequenceService.GetScale(tier)));
                ecb.AddComponent(spawnedItem, new SuikaItem
                {
                    Tier = tier,
                    Shape = DropperSpawnSequenceService.SphereShape,
                    SpawnIndex = nextSpawnIndex - 1
                });
            }

            config.ValueRW.NextSpawnIndex = nextSpawnIndex;
            ecb.RemoveComponent<DropperInitialSpawnRequest>(requestEntity);
        }


        static Entity ResolveSpherePrefabByDefinition(SuikaGameConfig config, byte tier)
        {
            return tier switch
            {
                2 => config.Sphere2PrefabEntity,
                1 => config.Sphere1PrefabEntity,
                _ => config.Sphere0PrefabEntity
            };
        }
    }
}