using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Code.InputHandling
{
    public static class GameInput
    {
        const string DefaultMapName = "Default";
        const float MoveDeadZone = 0.001f;
        const float AccelerometerDeadZone = 0.18f;
        const float AccelerometerScale = 0.7f;
        const float AccelerometerSmoothing = 1f;

        static InputActionMap s_DefaultMap;
        static InputAction Move;
        static float s_AccelerometerDeadZone = AccelerometerDeadZone;
        static float s_AccelerometerScale = AccelerometerScale;
        static float s_AccelerometerSmoothing = AccelerometerSmoothing;
        static InputAction Interact;
        static InputAction Click;
        static InputAction Submit;
        static bool SupportsMobileMotion;
        static float s_SmoothedAccelerometerTiltX;

        static GameInput() {
            Initialize();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnEnterPlayMode() {
            s_SmoothedAccelerometerTiltX = 0f;
            Initialize();
        }

        static void Initialize() {
            var actionsAsset = InputSystem.actions;
            if (actionsAsset == null)
                throw new InvalidOperationException("InputActionAsset not configured.");

            SupportsMobileMotion = Application.isMobilePlatform || Application.isEditor;

            if (SupportsMobileMotion) {
                var accelerometer = Accelerometer.current;
                if (accelerometer != null)
                    InputSystem.EnableDevice(accelerometer);
            }

            if (s_DefaultMap != null)
                s_DefaultMap.Disable();

            s_DefaultMap = actionsAsset.FindActionMap(DefaultMapName);

            Move = FindRequiredAction(s_DefaultMap, "Move");
            Interact = FindRequiredAction(s_DefaultMap, "Interact");
            Click = FindRequiredAction(s_DefaultMap, "Click");
            Submit = FindRequiredAction(s_DefaultMap, "Submit");

            s_DefaultMap.Enable();
        }

        static InputAction FindRequiredAction(InputActionMap map, string actionName) {
            return map.FindAction(actionName)
                   ?? throw new InvalidOperationException(
                       $"Required InputAction '{actionName}' missing in map '{map.name}'.");
        }

        public static float GetMoveX() {
            float moveX = Move.ReadValue<Vector2>().x;
            if (Mathf.Abs(moveX) > MoveDeadZone)
                return Mathf.Clamp(moveX, -1f, 1f);

            if (SupportsMobileMotion) {
                float tiltX = GetAccelerometerTiltXSmoothed();
                if (Mathf.Abs(tiltX) > s_AccelerometerDeadZone)
                    return Mathf.Clamp(tiltX, -1f, 1f);
            }

            return 0f;
        }

        public static bool DropPressedThisFrame() {
            return
                Interact.WasPressedThisFrame() ||
                Submit.WasPressedThisFrame() ||
                WasMouseClickPressedThisFrame() ||
                WasTouchscreenTappedThisFrame();
        }

        static bool WasMouseClickPressedThisFrame() {
            if (!Click.WasPressedThisFrame())
                return false;

            var mouse = Mouse.current;
            if (mouse == null)
                return true;

            return !IsPointerOverUI(mouse.position.ReadValue());
        }

        static bool IsPointerOverUI(Vector2 screenPosition) {
            var documents = Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude);
            for (var i = 0; i < documents.Length; i++) {
                var uiDocument = documents[i];
                var rootElement = uiDocument.rootVisualElement;
                if (rootElement == null)
                    continue;

                var panel = rootElement.panel;
                if (panel == null)
                    continue;

                var panelPosition = RuntimePanelUtils.ScreenToPanel(panel, screenPosition);

                if (panel.Pick(panelPosition) != null)
                    return true;
            }

            return false;
        }

        static float GetAccelerometerTiltXSmoothed() {
            var accelerometer = Accelerometer.current;
            if (accelerometer is null)
                return 0f;

            if (!accelerometer.enabled)
                InputSystem.EnableDevice(accelerometer);

            float rawTiltX = -accelerometer.acceleration.ReadValue().x / s_AccelerometerScale;
            if (Mathf.Abs(rawTiltX) < s_AccelerometerDeadZone)
                rawTiltX = 0f;

            s_SmoothedAccelerometerTiltX = Mathf.Lerp(s_SmoothedAccelerometerTiltX, rawTiltX, s_AccelerometerSmoothing);
            return s_SmoothedAccelerometerTiltX;
        }

        public static float GetAccelerometerDeadZone() {
            return s_AccelerometerDeadZone;
        }

        public static float GetAccelerometerScale() {
            return s_AccelerometerScale;
        }

        public static float GetAccelerometerSmoothing() {
            return s_AccelerometerSmoothing;
        }

        public static void ApplyAccelerometerSettings(float deadZone, float scale, float smoothing) {
            s_AccelerometerDeadZone = Mathf.Max(0f, deadZone);
            s_AccelerometerScale = Mathf.Max(0.0001f, scale);
            s_AccelerometerSmoothing = Mathf.Clamp01(smoothing);
            s_SmoothedAccelerometerTiltX = 0f;
        }

        static bool WasTouchscreenTappedThisFrame() {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null || !touchscreen.primaryTouch.press.wasPressedThisFrame)
                return false;

            return !IsPointerOverUI(touchscreen.primaryTouch.position.ReadValue());
        }
    }
}