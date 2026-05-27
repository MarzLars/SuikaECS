using System;
using Suika.UI;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SuikaScripts
{
    [DisallowMultipleComponent]
    public class SuikaGameConfigAuthoring : MonoBehaviour
    {
        [Tooltip("ScriptableObject containing all prefab definitions and game config")]
        public SuikaGameConfigData configData;

        [Header("Deterministic seed")] [Tooltip("Used for spawn sequence randomization")]
        public uint seed = DropperSpawnSequenceService.DefaultSeed;


        class SuikaGameConfigBaker : Baker<SuikaGameConfigAuthoring>
        {
            public override void Bake(SuikaGameConfigAuthoring authoring) {
                if (!authoring.configData)
                    return;

                var entity = GetEntity(TransformUsageFlags.None);
                BakeFromConfigData(entity, authoring);
            }

            void BakeFromConfigData(Entity entity, SuikaGameConfigAuthoring authoring) {
                var configData = authoring.configData;

                AddComponent(entity, new SuikaGameConfig {
                    Seed = authoring.seed,
                    NextSpawnIndex = 0
                });

                var burstBuffer = AddBuffer<SuikaPopConfigBuffer>(entity);
                var burstConfigs = configData.BurstBubbleConfigs ?? Array.Empty<SuikaPopConfig>();
                foreach (var burstConfig in burstConfigs) {
                    if (burstConfig is null) {
                        // Keep index parity with authoring array so PoppedBubbleConfigIndex remains stable.
                        burstBuffer.Add(SuikaPopConfigBuffer.Default);
                        continue;
                    }

                    burstBuffer.Add(new SuikaPopConfigBuffer {
                        SphereCount = Mathf.Max(0, burstConfig.SphereCount),
                        SpherePrefabEntity = burstConfig.SpherePrefab ?
                            GetEntity(burstConfig.SpherePrefab, TransformUsageFlags.Dynamic) :
                            Entity.Null,
                        SphereSize = Mathf.Max(0.01f, burstConfig.SphereSize),
                        Radius = Mathf.Max(0f, burstConfig.Radius),
                        SleepDelaySeconds = Mathf.Max(0f, burstConfig.SleepDelaySeconds)
                    });
                }

                // Create buffer with all tier definitions
                var tierBuffer = AddBuffer<SuikaPrefabTierBuffer>(entity);

                for (byte tier = 0; tier < configData.PrefabDefinitions.Length; tier++) {
                    var prefabDef = configData.PrefabDefinitions[tier];
                    if (!prefabDef.Prefab)
                        continue;

                    tierBuffer.Add(new SuikaPrefabTierBuffer {
                        Tier = tier,
                        Shape = prefabDef.Shape,
                        PrefabEntity = GetEntity(prefabDef.Prefab, TransformUsageFlags.Dynamic),
                        Scale = prefabDef.Scale,
                        Color = new float4(prefabDef.Color.r, prefabDef.Color.g, prefabDef.Color.b, prefabDef.Color.a),
                        ScoreValue = prefabDef.ScoreValue,
                        BurstOnMerge = prefabDef.BurstOnMerge ?
                            (byte)1 :
                            (byte)0,
                        BurstConfigIndex = prefabDef.BurstOnMerge ?
                            Mathf.Clamp(prefabDef.PoppedBubbleConfigIndex, 0, Mathf.Max(0, burstBuffer.Length - 1)) :
                            -1
                    });
                }

                // Initialize UI State components
                AddComponent(entity, new SuikaGameState { State = GameState.Init });
                AddComponent(entity, new SuikaScore { Value = 0 });
                AddComponent(entity, new DropperPreviewReady());
                SetComponentEnabled<DropperPreviewReady>(entity, true);
            }
        }
    }
}