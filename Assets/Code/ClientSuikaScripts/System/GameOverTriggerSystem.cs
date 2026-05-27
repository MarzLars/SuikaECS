using Suika.UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;

namespace SuikaScripts
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct GameOverTriggerSystem : ISystem
    {
        static float FlashAmount(float elapsedSeconds, float flashFrequencyHz) {
            float phase = elapsedSeconds * flashFrequencyHz * math.PI * 2f;
            return 0.5f - 0.5f * math.cos(phase);
        }

        static float4 LerpWarningColor(float4 defaultColor, float flashAmount) {
            return math.lerp(defaultColor, new float4(1f, 0f, 0f, defaultColor.w), flashAmount);
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<SuikaGameState>();
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SuikaGameOverTrigger>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state) {
            if (!SystemAPI.TryGetSingleton<SuikaGameState>(out var gameState))
                return;

            if (gameState.State != GameState.Playing)
                return;

            if (!SystemAPI.TryGetSingletonEntity<SuikaGameOverTrigger>(out var triggerEntity))
                return;

            var ecbSingleton = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var gameOverRequested = new NativeArray<byte>(1, Allocator.TempJob);

            state.Dependency = new GameOverTriggerJob {
                ECB = ecb.AsParallelWriter(),
                TriggerEntity = triggerEntity,
                DeltaTime = SystemAPI.Time.DeltaTime,
                TriggerLookup = SystemAPI.GetComponentLookup<SuikaGameOverTrigger>(true),
                TriggerTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                TriggerColliderLookup = SystemAPI.GetComponentLookup<PhysicsCollider>(true),
                WarningLookup = SystemAPI.GetComponentLookup<SuikaGameOverWarning>(),
                LinkedLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(true),
                ColorLookup = SystemAPI.GetComponentLookup<SuikaColorOverride>(),
                EmissionLookup = SystemAPI.GetComponentLookup<URPMaterialPropertyEmissionColor>(),
                GameOverRequested = gameOverRequested
            }.Schedule(state.Dependency);

            state.Dependency = gameOverRequested.Dispose(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(SuikaItem))]
        partial struct GameOverTriggerJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public Entity TriggerEntity;
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<SuikaGameOverTrigger> TriggerLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TriggerTransformLookup;
            [ReadOnly] public ComponentLookup<PhysicsCollider> TriggerColliderLookup;
            public ComponentLookup<SuikaGameOverWarning> WarningLookup;
            [ReadOnly] public BufferLookup<LinkedEntityGroup> LinkedLookup;
            public ComponentLookup<SuikaColorOverride> ColorLookup;
            public ComponentLookup<URPMaterialPropertyEmissionColor> EmissionLookup;
            public NativeArray<byte> GameOverRequested;

            void Execute(
                Entity entity,
                [ChunkIndexInQuery] int chunkIndex,
                in LocalTransform transform,
                in PhysicsCollider collider) {
                if (!ColorLookup.HasComponent(entity))
                    return;

                if (!TriggerLookup.HasComponent(TriggerEntity) ||
                    !TriggerTransformLookup.HasComponent(TriggerEntity) ||
                    !TriggerColliderLookup.HasComponent(TriggerEntity))
                    return;

                var triggerSettings = TriggerLookup[TriggerEntity];
                var triggerTransform = TriggerTransformLookup[TriggerEntity];
                var triggerCollider = TriggerColliderLookup[TriggerEntity];
                var triggerAabb = triggerCollider.Value.Value.CalculateAabb(
                    new RigidTransform(triggerTransform.Rotation, triggerTransform.Position),
                    triggerTransform.Scale);

                float triggerBottomY = triggerAabb.Min.y + triggerSettings.BottomSegmentOffsetY;
                float triggerTopY = triggerAabb.Max.y;
                float warningDuration = math.max(0.1f, triggerSettings.WarningDurationSeconds);
                float flashFrequency = math.max(0.1f, triggerSettings.FlashFrequencyHz);

                var itemAabb = collider.Value.Value.CalculateAabb(
                    new RigidTransform(transform.Rotation, transform.Position),
                    transform.Scale);

                bool isInTrigger = itemAabb.Max.y > triggerBottomY && itemAabb.Min.y < triggerTopY;

                if (!isInTrigger) {
                    if (!WarningLookup.HasComponent(entity)) return;
                    var exitedWarning = WarningLookup[entity];
                    SetVisual(entity, chunkIndex, exitedWarning.DefaultColor, exitedWarning.DefaultEmission);
                    ECB.RemoveComponent<SuikaGameOverWarning>(chunkIndex, entity);

                    return;
                }

                bool hasWarning = WarningLookup.HasComponent(entity);

                if (!hasWarning) {
                    var defaultColor = ColorLookup[entity].Value;
                    ECB.AddComponent(chunkIndex, entity, new SuikaGameOverWarning {
                        SecondsRemaining = warningDuration,
                        FlashElapsedSeconds = 0f,
                        DefaultColor = defaultColor,
                        DefaultEmission = GetEmission(entity)
                    });
                    SetVisual(entity, chunkIndex, LerpWarningColor(defaultColor, 1f), WarningEmission(1f));
                    return;
                }

                var warning = WarningLookup[entity];
                warning.SecondsRemaining -= DeltaTime;
                warning.FlashElapsedSeconds += DeltaTime;

                if (warning.SecondsRemaining <= 0f) {
                    SetVisual(entity, chunkIndex, warning.DefaultColor, warning.DefaultEmission);
                    ECB.RemoveComponent<SuikaGameOverWarning>(chunkIndex, entity);

                    if (GameOverRequested[0] == 0) {
                        var eventEntity = ECB.CreateEntity(chunkIndex);
                        ECB.AddComponent(chunkIndex, eventEntity, new GameOverEvent());
                        ECB.AddComponent(chunkIndex, eventEntity, new Event());
                        GameOverRequested[0] = 1;
                    }

                    return;
                }

                float flashAmount = FlashAmount(warning.FlashElapsedSeconds, flashFrequency);
                SetVisual(entity, chunkIndex, LerpWarningColor(warning.DefaultColor, flashAmount),
                    WarningEmission(flashAmount));
                WarningLookup[entity] = warning;
            }

            float4 GetEmission(Entity entity) {
                return EmissionLookup.HasComponent(entity) ? EmissionLookup[entity].Value : float4.zero;
            }

            static float4 WarningEmission(float flashAmount) {
                return new float4(8f * flashAmount, 0f, 0f, 1f);
            }

            void SetVisual(Entity entity, int chunkIndex, float4 newColor, float4 newEmission) {
                SetColor(entity, chunkIndex, newColor);
                SetEmission(entity, chunkIndex, newEmission);
            }

            void SetColor(Entity entity, int chunkIndex, float4 newColor) {
                var color = new SuikaColorOverride { Value = newColor };
                if (ColorLookup.HasComponent(entity))
                    ColorLookup[entity] = color;
                else
                    ECB.AddComponent(chunkIndex, entity, color);

                if (!LinkedLookup.HasBuffer(entity))
                    return;

                var buf = LinkedLookup[entity];
                for (var i = 0; i < buf.Length; i++) {
                    var linked = buf[i].Value;
                    if (linked == entity)
                        continue;
                    if (ColorLookup.HasComponent(linked))
                        ColorLookup[linked] = color;
                    else
                        ECB.AddComponent(chunkIndex, linked, color);
                }
            }

            void SetEmission(Entity entity, int chunkIndex, float4 newEmission) {
                var emission = new URPMaterialPropertyEmissionColor { Value = newEmission };
                if (EmissionLookup.HasComponent(entity))
                    EmissionLookup[entity] = emission;
                else
                    ECB.AddComponent(chunkIndex, entity, emission);

                if (!LinkedLookup.HasBuffer(entity))
                    return;

                var buf = LinkedLookup[entity];
                for (var i = 0; i < buf.Length; i++) {
                    var linked = buf[i].Value;
                    if (linked == entity)
                        continue;
                    if (EmissionLookup.HasComponent(linked))
                        EmissionLookup[linked] = emission;
                    else
                        ECB.AddComponent(chunkIndex, linked, emission);
                }
            }
        }
    }
}