using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;

namespace SuikaScripts
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DropperSpawnSystem))]
    [BurstCompile]
    public partial struct DropperPreviewSystem : ISystem
    {
        ComponentLookup<DropperPreviewEntity> _previewLookup;
        ComponentLookup<DropperPreviewState> _previewStateLookup;
        BufferLookup<SuikaPrefabTierBuffer> _tierBufferLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SuikaGameConfig>();
            state.RequireForUpdate<DropperSpawnPoint>();
            state.RequireForUpdate<SuikaPrefabTierBuffer>();
            state.RequireForUpdate<BeginPresentationEntityCommandBufferSystem.Singleton>();

            _previewLookup = state.GetComponentLookup<DropperPreviewEntity>();
            _previewStateLookup = state.GetComponentLookup<DropperPreviewState>();
            _tierBufferLookup = state.GetBufferLookup<SuikaPrefabTierBuffer>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            _previewLookup.Update(ref state);
            _previewStateLookup.Update(ref state);
            _tierBufferLookup.Update(ref state);

            var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            var config = SystemAPI.GetSingleton<SuikaGameConfig>();
            var configEntity = SystemAPI.GetSingletonEntity<SuikaGameConfig>();
            var spawnPointEntity = SystemAPI.GetSingletonEntity<DropperSpawnPoint>();
            var spawnPoint = SystemAPI.GetSingleton<DropperSpawnPoint>();

            if (!_tierBufferLookup.TryGetBuffer(configEntity, out var tierBuffer))
                return;

            bool previewReady = SystemAPI.IsComponentEnabled<DropperPreviewReady>(configEntity);
            bool spawnBlocked = SystemAPI.HasComponent<DropperSpawnBlocked>(configEntity);

            // Preview only appears after collision gate opened.
            if (!previewReady || spawnBlocked) {
                var blockedPreview = _previewLookup[spawnPointEntity];
                if (blockedPreview.Value != Entity.Null) {
                    // Gate closed: destroy preview entity completely so no visual residue remains.
                    ecb.DestroyEntity(blockedPreview.Value);
                    ecb.SetComponent(spawnPointEntity, new DropperPreviewEntity { Value = Entity.Null });
                }

                return;
            }

            // Determine next tier to spawn
            uint seed = config.Seed;
            int nextSpawnIndex = config.NextSpawnIndex;
            byte nextTier = DropperSpawnSequenceService.GetTier(seed, nextSpawnIndex);

            if (!DropperSpawnSequenceService.TryGetTierDefinition(tierBuffer, nextTier, out var tierDefinition))
                return;

            if (tierDefinition.PrefabEntity == Entity.Null)
                return;

            // Get or create preview entity
            var currentPreview = _previewLookup[spawnPointEntity];

            // Check if we need to update the preview (new tier or first time)
            bool needsUpdate = currentPreview.Value == Entity.Null;

            if (!needsUpdate && _previewStateLookup.TryGetComponent(currentPreview.Value, out var previewState)) {
                if (previewState.Tier != nextTier || previewState.Shape != tierDefinition.Shape) {
                    // Tier/shape changed, destroy old and create new
                    ecb.DestroyEntity(currentPreview.Value);
                    needsUpdate = true;
                }
            }
            else if (!needsUpdate) {
                needsUpdate = true;
            }

            if (needsUpdate) {
                // Create new preview
                var previewEntity = ecb.Instantiate(tierDefinition.PrefabEntity);
                ecb.AddComponent(previewEntity, new DropperPreviewTag());
                ecb.AddComponent(previewEntity, new DropperPreviewState {
                    Tier = tierDefinition.Tier,
                    Shape = tierDefinition.Shape
                });

                // Remove gameplay/physics components so preview only renders and doesn't interact
                ecb.RemoveComponent<SuikaItem>(previewEntity);
                ecb.RemoveComponent<PoppedBubble>(previewEntity);
                ecb.RemoveComponent<PoppedBubbleSleepTimer>(previewEntity);
                ecb.RemoveComponent<PoppedBubbleStaticTag>(previewEntity);
                ecb.RemoveComponent<PhysicsVelocity>(previewEntity);
                ecb.RemoveComponent<PhysicsMass>(previewEntity);
                ecb.RemoveComponent<PhysicsDamping>(previewEntity);
                ecb.RemoveComponent<PhysicsGravityFactor>(previewEntity);
                ecb.RemoveComponent<PhysicsCollider>(previewEntity);

                // Set initial position and appearance
                float scale = GetPreviewScale(tierDefinition.Shape, tierDefinition.Tier);
                ecb.SetComponent(previewEntity, LocalTransform.FromPositionRotationScale(
                    spawnPoint.Position,
                    quaternion.identity,
                    tierDefinition.Shape == DropperSpawnSequenceService.SphereShape ? scale : 1f));

                var previewColor = tierDefinition.Color;
                previewColor.w *= 0.45f; // make preview semi-transparent
                ecb.SetComponent(previewEntity, new SuikaColorOverride { Value = previewColor });

                if (tierDefinition.Shape == DropperSpawnSequenceService.CylinderShape) {
                    ecb.RemoveComponent<PostTransformMatrix>(previewEntity);
                    ecb.AddComponent(previewEntity, new PostTransformMatrix {
                        Value = float4x4.Scale(scale, scale, 1f)
                    });
                }

                // Update reference
                ecb.SetComponent(spawnPointEntity, new DropperPreviewEntity { Value = previewEntity });
            }
            else if (currentPreview.Value != Entity.Null) {
                if (SystemAPI.HasComponent<DisableRendering>(currentPreview.Value))
                    ecb.RemoveComponent<DisableRendering>(currentPreview.Value);

                // Update position if dropper moved
                ecb.SetComponent(currentPreview.Value, new LocalTransform {
                    Position = spawnPoint.Position,
                    Rotation = quaternion.identity,
                    Scale = tierDefinition.Shape == DropperSpawnSequenceService.SphereShape ?
                        GetPreviewScale(tierDefinition.Shape, tierDefinition.Tier) :
                        1f
                });
            }
        }

        static float GetPreviewScale(byte shape, byte tier) {
            return DropperSpawnSequenceService.GetScale(tier);
        }
    }
}