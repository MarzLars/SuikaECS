using System;
using UnityEngine;

namespace SuikaScripts
{
    /// <summary>
    ///     ScriptableObject configuration for Suika game prefabs and their properties.
    ///     Inspired by Unity.DotsUISample's QuestData pattern.
    ///     The order of elements in the PrefabDefinitions array determines the tier (index 0 = tier 0, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "SuikaGameConfig", menuName = "Suika/Game Config")]
    public class SuikaGameConfigData : ScriptableObject
    {
        [Header("Prefab Definitions (Order determines tier level)")]
        [Tooltip("Array of prefab definitions. Tier is determined by array index (0-based).")]
        public SuikaPrefabDefinition[] PrefabDefinitions = Array.Empty<SuikaPrefabDefinition>();

        [Header("Burst Bubble Configurations")]
        [Tooltip("Reusable burst presets that can be selected by prefab tiers with BurstOnMerge enabled.")]
        public SuikaPopConfig[] BurstBubbleConfigs = Array.Empty<SuikaPopConfig>();
    }

    /// <summary>
    ///     Defines properties for a single prefab tier.
    ///     Tier is determined by the array index in SuikaGameConfigData.PrefabDefinitions
    /// </summary>
    [Serializable]
    public class SuikaPrefabDefinition
    {
        [Tooltip("The prefab to spawn for this tier")]
        public GameObject Prefab;

        [Tooltip("Shape type: 0 = Sphere, 1 = Cylinder")] [Range(0, 1)]
        public byte Shape = DropperSpawnSequenceService.SphereShape;

        public float Scale = 0.5f;

        [Tooltip("Color for this tier. Requires a Material with _BaseColor support.")]
        public Color Color = Color.white;

        [Tooltip("Score points awarded when this tier merges")] [Min(0)]
        public int ScoreValue;

        [Tooltip(
            "If enabled, merging two items of this tier spawns the burst spheres instead of normal merge promotion.")]
        public bool BurstOnMerge;

        [Tooltip("Index into Burst Configurations. Used only when BurstOnMerge is enabled.")] [Min(0)]
        public int PoppedBubbleConfigIndex;
    }

    [Serializable]
    public class SuikaPopConfig
    {
        [Tooltip("Friendly label for identifying this burst configuration in the inspector.")]
        public string Name = "PoppedBubble";

        [Tooltip("How many non-mergeable spheres this burst spawns.")] [Min(0)]
        public int SphereCount = 8;

        [Tooltip("Optional custom sphere prefab for this burst. If empty, a sphere tier prefab is used.")]
        public GameObject SpherePrefab;

        [Tooltip("Multiplier applied to burst sphere size when spawned.")] [Min(0.01f)]
        public float SphereSize = 1f;

        [Tooltip("Radius around the merge point where burst spheres are spawned.")] [Min(0f)]
        public float Radius = 0.6f;

        [Tooltip("Seconds after spawn before burst spheres are forced into sleep behavior.")] [Min(0f)]
        public float SleepDelaySeconds = 3f;
    }
}