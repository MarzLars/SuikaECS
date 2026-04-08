using Unity.Entities;
using UnityEngine;

namespace Code.CodeMonkey_EcsTutorial.PrefabSpawning
{
    public class SpawnCubeConfigAuthoring : MonoBehaviour
    {
        public GameObject cubePrefab;
        public int amountToSpawn;

        class Baker : Baker<SpawnCubeConfigAuthoring>
        {
            public override void Bake(SpawnCubeConfigAuthoring authoring) {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new SpawnCubeConfig {
                    CubePrefabEntity = GetEntity(authoring.cubePrefab, TransformUsageFlags.Dynamic),
                    AmountToSpawn = authoring.amountToSpawn
                });
            }
        }
    }

    public struct SpawnCubeConfig : IComponentData
    {
        public Entity CubePrefabEntity;
        public int AmountToSpawn;
    }
}