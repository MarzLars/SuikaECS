using Unity.Entities;
using UnityEngine;

namespace SuikaScripts
{
    [DisallowMultipleComponent]
    public class SuikaGameConfigAuthoring : MonoBehaviour
    {
        [Header("Deterministic seed")] public uint seed = DropperSpawnSequenceService.DefaultSeed;

        [Header("Sphere prefabs")]
        public GameObject sphere0Prefab;
        public GameObject sphere1Prefab;
        public GameObject sphere2Prefab;

        [Header("Cylinder prefabs")]
        public GameObject cylinder3Prefab;
        public GameObject cylinder4Prefab;
        public GameObject cylinder5Prefab;

        [Min(1)] public int initialSpawnCount = 1;

        class SuikaGameConfigBaker : Baker<SuikaGameConfigAuthoring>
        {
            public override void Bake(SuikaGameConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SuikaGameConfig
                {
                    Seed = authoring.seed,
                    NextSpawnIndex = 0,
                    Sphere0PrefabEntity = GetEntity(authoring.sphere0Prefab, TransformUsageFlags.Dynamic),
                    Sphere1PrefabEntity = GetEntity(authoring.sphere1Prefab, TransformUsageFlags.Dynamic),
                    Sphere2PrefabEntity = GetEntity(authoring.sphere2Prefab, TransformUsageFlags.Dynamic),
                    Cylinder3PrefabEntity = GetEntity(authoring.cylinder3Prefab, TransformUsageFlags.Dynamic),
                    Cylinder4PrefabEntity = GetEntity(authoring.cylinder4Prefab, TransformUsageFlags.Dynamic),
                    Cylinder5PrefabEntity = GetEntity(authoring.cylinder5Prefab, TransformUsageFlags.Dynamic)
                });
                AddComponent(entity, new DropperInitialSpawnRequest
                {
                    Count = Mathf.Max(1, authoring.initialSpawnCount)
                });
            }
        }
    }
}