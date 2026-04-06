using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = UnityEngine.Random;

namespace Code.CodeMonkey_EcsTutorial.PrefabSpawning
{
    public partial class SpawnCubeSystem : SystemBase
    {
        protected override void OnCreate() {
            RequireForUpdate<SpawnCubeConfig>();
        }

        protected override void OnUpdate() {
            Enabled = false;

            var spawnCubeConfig = SystemAPI.GetSingleton<SpawnCubeConfig>();

            for (var i = 0; i < spawnCubeConfig.amountToSpawn; i++) {
                var spawnedEntity = EntityManager.Instantiate(spawnCubeConfig.cubePrefabEntity);

                SystemAPI.SetComponent(spawnedEntity,
                    new
                        LocalTransform //Could also do EntityManager.SetComponentData, but SystemAPI.SetComponent(spawnedEntity, new LocalTransform) is more efficient
                        {
                            Position = new float3(
                                Random.Range(-10f, 10f),
                                Random.Range(0f, 10f),
                                Random.Range(-10f, 10f)
                            ),
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });
            }
        }
    }
}