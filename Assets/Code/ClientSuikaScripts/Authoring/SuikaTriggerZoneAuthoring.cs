using Unity.Entities;
using UnityEngine;

namespace SuikaScripts
{
    /// <summary>
    ///     Marks a collider as a trigger zone that causes nearby items to flash.
    ///     Attach this MonoBehaviour to the GameObject containing the trigger collider.
    /// </summary>
    public sealed class SuikaTriggerZoneAuthoring : MonoBehaviour
    {
        public sealed class Baker : Baker<SuikaTriggerZoneAuthoring>
        {
            public override void Bake(SuikaTriggerZoneAuthoring authoring) {
                // Trigger-zone ECS component was removed; keep baker as no-op for scene compatibility.
            }
        }
    }
}