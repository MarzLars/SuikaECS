using System;
using Code.CodeMonkey_EcsTutorial.PrefabSpawning;
using Code.CodeMonkey_EcsTutorial.RotationAndMovement.Components.Tags;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Code.CodeMonkey_EcsTutorial.RotationAndMovement.Systems
{
    public partial class PlayerShootingSystem : SystemBase
    {
        public event EventHandler OnShoot;

        protected override void OnCreate() {
            RequireForUpdate<Player>();
        }

        protected override void OnUpdate() {
            if (!Input.GetMouseButton(0)) return;

            var spawnCubeConfig = SystemAPI.GetSingleton<SpawnCubeConfig>();

            var entityCommandBuffer = new EntityCommandBuffer(WorldUpdateAllocator);

            foreach (var (localTransform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                         .WithAll<Player>()
                         .WithEntityAccess()) {
                var spawnedEntity = entityCommandBuffer.Instantiate(spawnCubeConfig.cubePrefabEntity);
                entityCommandBuffer.SetComponent(spawnedEntity, new LocalTransform {
                    Position = localTransform.ValueRO.Position,
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                OnShoot?.Invoke(this, EventArgs.Empty);
                PlayerShootManager.Instance.PlayerShoot(localTransform.ValueRO.Position);
            }

            entityCommandBuffer.Playback(EntityManager);
        }
    }
}