using Suika.UI;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace SuikaScripts
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(SuikaUISystem))]
    public partial struct DropperVisualPresentationSystem : ISystem
    {
        public void OnCreate(ref SystemState state) {
            state.RequireForUpdate<DropperSpawnPoint>();
            state.RequireForUpdate<DropperAimConfig>();
            state.RequireForUpdate<DropperRaycastVisualConfig>();
            state.RequireForUpdate<UIScreens>();
        }

        public void OnUpdate(ref SystemState state) {
            if (!SystemAPI.TryGetSingleton<UIScreens>(out var screensData))
                return;

            var camera = Camera.main;
            if (!camera)
                return;

            var hud = screensData.HUDScreen.Value;
            if (!hud || hud.RootElement?.panel is null)
                return;

            var hasDropper = false;
            float3 dropperPosition = default;
            DropperAimConfig aimConfig = default;
            DropperRaycastVisualConfig raycastConfig = default;

            foreach (var (spawnPoint, config, rayVisual) in SystemAPI
                         .Query<RefRO<DropperSpawnPoint>, RefRO<DropperAimConfig>,
                             RefRO<DropperRaycastVisualConfig>>()) {
                dropperPosition = spawnPoint.ValueRO.Position;
                aimConfig = config.ValueRO;
                raycastConfig = rayVisual.ValueRO;
                hasDropper = true;
                break;
            }

            if (!hasDropper)
                return;

            var markerPanelPosition = WorldToPanelPosition(hud.RootElement, camera, dropperPosition);
            var minPanelPosition = WorldToPanelPosition(hud.RootElement, camera,
                new float3(aimConfig.MinX, dropperPosition.y, dropperPosition.z));
            var maxPanelPosition = WorldToPanelPosition(hud.RootElement, camera,
                new float3(aimConfig.MaxX, dropperPosition.y, dropperPosition.z));

            hud.SetDropperAimVisual(
                markerPanelPosition.x,
                markerPanelPosition.y,
                minPanelPosition.x,
                maxPanelPosition.x,
                markerPanelPosition.y);

            float rayDistance = math.max(0.1f, raycastConfig.MaxDistance);
            var origin = new Vector3(dropperPosition.x, dropperPosition.y, dropperPosition.z);
            var direction = Vector3.down;

#if UNITY_EDITOR
            var end = origin + direction * rayDistance;
            if (Physics.Raycast(origin, direction, out var hitInfo, rayDistance, raycastConfig.LayerMask))
                end = hitInfo.point;

            // Editor-only visualisation using Debug.DrawLine (no managed allocations at runtime)
            Debug.DrawLine(origin, end, Color.cyan, 0f, false);
            DrawEditorDebugLines(origin, end, aimConfig, dropperPosition);
#endif
        }

        static Vector2 WorldToPanelPosition(VisualElement rootElement, Camera camera, float3 worldPosition) {
            var screenPosition =
                camera.WorldToScreenPoint(new Vector3(worldPosition.x, worldPosition.y, worldPosition.z));
            screenPosition.y = Screen.height - screenPosition.y;
            return RuntimePanelUtils.ScreenToPanel(rootElement.panel, new Vector2(screenPosition.x, screenPosition.y));
        }

        // No managed LineRenderer or GameObject creation in system. Presentation handled via HUD and
        // editor-only debug draw above. For runtime visual effects, use a dedicated MonoBehaviour
        // presenter that reads ECS data and manages managed Unity objects.

#if UNITY_EDITOR
        static void DrawEditorDebugLines(Vector3 origin, Vector3 rayEnd, DropperAimConfig aimConfig,
            float3 dropperPosition) {
            Debug.DrawLine(origin, rayEnd, Color.cyan, 0f, false);

            var boundsLeft = new Vector3(aimConfig.MinX, dropperPosition.y, dropperPosition.z);
            var boundsRight = new Vector3(aimConfig.MaxX, dropperPosition.y, dropperPosition.z);
            Debug.DrawLine(boundsLeft, boundsRight, Color.yellow, 0f, false);

            const float markerHalfSize = 0.15f;
            var markerLeft = new Vector3(dropperPosition.x - markerHalfSize, dropperPosition.y, dropperPosition.z);
            var markerRight = new Vector3(dropperPosition.x + markerHalfSize, dropperPosition.y, dropperPosition.z);
            var markerTop = new Vector3(dropperPosition.x, dropperPosition.y + markerHalfSize, dropperPosition.z);
            var markerBottom = new Vector3(dropperPosition.x, dropperPosition.y - markerHalfSize, dropperPosition.z);
            Debug.DrawLine(markerLeft, markerRight, Color.green, 0f, false);
            Debug.DrawLine(markerTop, markerBottom, Color.green, 0f, false);
        }
#endif
    }
}