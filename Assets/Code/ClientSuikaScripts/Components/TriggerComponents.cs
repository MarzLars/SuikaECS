// REMOVED: SuikaTriggerZone, SuikaItemInTrigger
//
// REASON: Both components were only used by SuikaTriggerFlashCollisionSystem and
// SuikaItemTriggerFlashSystem, which have been removed.
// GameOverTriggerSystem uses an AABB overlap check and does not need these markers.
//
// ACTION REQUIRED: Remove the SuikaTriggerZoneAuthoring MonoBehaviour from any
// GameObjects in your scene to avoid "Missing Script" warnings.

