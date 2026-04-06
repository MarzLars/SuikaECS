using System;
using InputHandling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Code.InputHandling
{
    [CreateAssetMenu(fileName = "InputReader", menuName = "Scriptable Objects/InputReader")]
    public class InputReaderSO : ScriptableObject, InputSystem_Actions.IDefaultActions
    {
        const string k_BindingsKey = "InputBindings";
        const string k_LookSensKey = "LookSensitivity";
        const string k_ScrollSensKey = "ScrollSensitivity";
        const string k_PanSensKey = "PanSensitivity";
        const string k_TurboMultiplierKey = "TurboMultiplier";
        const float k_DefaultLookSensitivity = 1f;
        const float k_DefaultScrollSensitivity = 1f;
        const float k_DefaultPanSensitivity = 1f;
        const float k_DefaultTurboMultiplier = 3f;

        public bool IsUIEngaged;

        InputSystem_Actions _inputActions;

        float _lookSensitivity = k_DefaultLookSensitivity;
        float _panSensitivity = k_DefaultPanSensitivity;
        float _scrollSensitivity = k_DefaultScrollSensitivity;
        float _turboMultiplier = k_DefaultTurboMultiplier;
        UIDocument _uiDocument; // Cached at runtime via RegisterUIDocument

        // Exposes the live InputActionAsset so SettingsUI can bind to the same instance
        public InputActionAsset Asset => _inputActions?.asset;

        /// <summary>Look mouse-delta multiplier. Setting auto-saves to PlayerPrefs.</summary>
        public float LookSensitivity {
            get => _lookSensitivity;
            set {
                _lookSensitivity = value;
                PlayerPrefs.SetFloat(k_LookSensKey, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Scroll wheel multiplier. Setting auto-saves to PlayerPrefs.</summary>
        public float ScrollSensitivity {
            get => _scrollSensitivity;
            set {
                _scrollSensitivity = value;
                PlayerPrefs.SetFloat(k_ScrollSensKey, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Camera pan drag multiplier. Setting auto-saves to PlayerPrefs.</summary>
        public float PanSensitivity {
            get => _panSensitivity;
            set {
                _panSensitivity = value;
                PlayerPrefs.SetFloat(k_PanSensKey, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Turbo speed multiplier applied while the turbo input is held.</summary>
        public float TurboMultiplier {
            get => _turboMultiplier;
            set {
                _turboMultiplier = value;
                PlayerPrefs.SetFloat(k_TurboMultiplierKey, value);
                PlayerPrefs.Save();
            }
        }

        public bool SuppressScrollInput { get; set; }

        public bool IsSettingsMenuOpen { get; set; }

        public float ScrollInput {
            get {
                if (_inputActions == null)
                    return 0f;

                if (SuppressScrollInput)
                    return 0f;

                if (IsPointerOverBlockedZoomElement())
                    return 0f;

                return _inputActions.Default.ScrollWheel.ReadValue<Vector2>().y * _scrollSensitivity;
            }
        }

        #region Events

        // Player Action Events
        public event Action<Vector2> MoveEvent = delegate { };
        public event Action<bool> ClickEvent = delegate { };
        public event Action<bool> HighlightObjectEvent = delegate { };
        public event Action<bool> PanEvent = delegate { };
        public event Action<bool> PlayEvent = delegate { };
        public event Action<bool> StepForwardEvent = delegate { };
        public event Action<bool> StepBackwardEvent = delegate { };
        public event Action<bool> TurboEvent = delegate { };
        public event Action<bool> SwapRenderModeEvent = delegate { };
        public event Action SettingsEvent = delegate { };

        #endregion

        #region Lifecycle

        #region UITK Registration and Pointer Capture

        /// <summary>
        ///     Register the runtime UIDocument to receive pointer capture callbacks.
        ///     When a UI element captures the pointer (e.g. slider drag), Click and Pan events
        ///     are force-released so world input is suppressed for the duration.
        ///     Call from any MonoBehaviour that owns the UIDocument (e.g. in its OnEnable).
        /// </summary>
        public void RegisterUIDocument(UIDocument uiDocument) {
            if (_uiDocument?.rootVisualElement != null)
                _uiDocument.rootVisualElement.UnregisterCallback<PointerCaptureEvent>(OnPointerCaptured);

            _uiDocument = uiDocument;
            var root = _uiDocument?.rootVisualElement;

            if (root == null)
                return;

            // Track pointer capture — when a UI element captures the pointer (e.g. slider drag),
            // world input should stay blocked even if the cursor leaves the element bounds.
            root.RegisterCallback<PointerCaptureEvent>(OnPointerCaptured);
        }

        void OnPointerCaptured(PointerCaptureEvent pointerCaptureEvent) {
            // A UI element just grabbed the pointer — force-release any active world interactions.
            // This closes the race where Click fires before UITK captures.
            ClickEvent.Invoke(false);
            PanEvent.Invoke(false);
        }

        #endregion

        public void EnablePlayerActions() {
            if (_inputActions == null) _inputActions = new InputSystem_Actions();

            _inputActions.Default.SetCallbacks(this);
            LoadBindingOverrides();
            _inputActions.Default.Enable();
        }

        public void SaveBindingOverrides() {
            if (_inputActions == null)
                return;

            PlayerPrefs.SetString(k_BindingsKey, _inputActions.asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public void ResetBindingOverrides() {
            if (_inputActions == null)
                return;

            _inputActions.asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(k_BindingsKey);
            PlayerPrefs.Save();
        }

        public void ResetBindingOverrides(InputAction action) {
            if (action == null)
                return;

            action.RemoveAllBindingOverrides();
            SaveBindingOverrides();
        }

        public void ResetBindingOverride(InputAction action, int bindingIndex) {
            if (action == null)
                return;

            action.RemoveBindingOverride(bindingIndex);
            SaveBindingOverrides();
        }

        public void ResetLookSensitivity() {
            LookSensitivity = k_DefaultLookSensitivity;
        }

        public void ResetScrollSensitivity() {
            ScrollSensitivity = k_DefaultScrollSensitivity;
        }

        public void ResetPanSensitivity() {
            PanSensitivity = k_DefaultPanSensitivity;
        }

        public void ResetTurboMultiplier() {
            TurboMultiplier = k_DefaultTurboMultiplier;
        }

        void LoadBindingOverrides() {
            if (_inputActions == null)
                return;

            if (PlayerPrefs.HasKey(k_BindingsKey))
                _inputActions.asset.LoadBindingOverridesFromJson(PlayerPrefs.GetString(k_BindingsKey));

            _lookSensitivity = PlayerPrefs.GetFloat(k_LookSensKey, k_DefaultLookSensitivity);
            _scrollSensitivity = PlayerPrefs.GetFloat(k_ScrollSensKey, k_DefaultScrollSensitivity);
            _panSensitivity = PlayerPrefs.GetFloat(k_PanSensKey, k_DefaultPanSensitivity);
            _turboMultiplier = PlayerPrefs.GetFloat(k_TurboMultiplierKey, k_DefaultTurboMultiplier);
        }

        public void DisablePlayerActions() {
            // Unregister old callbacks
            var oldRoot = _uiDocument?.rootVisualElement;
            oldRoot?.UnregisterCallback<PointerCaptureEvent>(OnPointerCaptured);

            _inputActions?.Default.Disable();
            _inputActions?.Default.Disable();
        }

        bool IsPointerOverBlockedZoomElement() {
            var root = _uiDocument?.rootVisualElement;
            if (root == null || root.panel == null || Mouse.current == null)
                return false;

            var panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, Mouse.current.position.ReadValue());
            var hoveredElement = root.panel.Pick(panelPosition);

            while (hoveredElement != null) {
                if (MatchesBlockedZoomName(hoveredElement.name) || MatchesBlockedZoomName(hoveredElement.viewDataKey))
                    return true;

                hoveredElement = hoveredElement.parent;
            }

            return false;
        }

        bool IsSubmitFocusedOnObjectList(InputAction.CallbackContext context) {
            if (!context.started && !context.performed)
                return false;

            var root = _uiDocument?.rootVisualElement;
            var focusedElement = root?.panel?.focusController?.focusedElement as VisualElement;

            while (focusedElement != null) {
                if (MatchesObjectListName(focusedElement.name) || MatchesObjectListName(focusedElement.viewDataKey))
                    return true;

                focusedElement = focusedElement.parent;
            }

            return false;
        }

        static bool MatchesBlockedZoomName(string value) {
            return !string.IsNullOrEmpty(value) &&
                   (value.Contains("zoom", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("slider", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("scroll", StringComparison.OrdinalIgnoreCase));
        }

        static bool MatchesObjectListName(string value) {
            return !string.IsNullOrEmpty(value) &&
                   (value.Contains("cluster", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("objectlist", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("object", StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region IUserDefaultActions

        #region Input Action Callbacks

        public void OnMove(InputAction.CallbackContext context) {
            MoveEvent?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnLook(InputAction.CallbackContext context) { } // Mouse position consumed via polling

        public void OnAttack(InputAction.CallbackContext context) {
            ClickEvent.Invoke(context.ReadValueAsButton());
        }

        public void OnInteract(InputAction.CallbackContext context) { }

        public void OnCrouch(InputAction.CallbackContext context) { }

        public void OnJump(InputAction.CallbackContext context) { }

        public void OnPrevious(InputAction.CallbackContext context) {
            StepBackwardEvent?.Invoke(context.performed);
        }

        public void OnNext(InputAction.CallbackContext context) {
            StepForwardEvent?.Invoke(context.performed);
        }

        public void OnSprint(InputAction.CallbackContext context) {
            TurboEvent.Invoke(context.performed);
        }

        public void OnNavigate(InputAction.CallbackContext context) { }

        public void OnSubmit(InputAction.CallbackContext context) { }

        public void OnCancel(InputAction.CallbackContext context) { }

        public void OnPoint(InputAction.CallbackContext context) { }

        public void OnClick(InputAction.CallbackContext context) {
            bool isPressed = context.ReadValueAsButton();

            // When Submit activates a focused ClusterList button, let UI own the action and
            // suppress world click sampling so selected ID doesn't get immediately overwritten.
            if (isPressed && IsSubmitFocusedOnObjectList(context))
                return;

            ClickEvent.Invoke(isPressed);
        }

        public void OnRightClick(InputAction.CallbackContext context) { }

        public void OnMiddleClick(InputAction.CallbackContext context) { }

        public void OnScrollWheel(InputAction.CallbackContext context) { }

        public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }

        public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }

        public void OnPan(InputAction.CallbackContext context) {
            PanEvent.Invoke(context.ReadValueAsButton());
        }

        public void OnTurbo(InputAction.CallbackContext context) {
            TurboEvent.Invoke(context.performed);
        }


        public void OnSwapRenderMode(InputAction.CallbackContext context) {
            SwapRenderModeEvent.Invoke(context.performed);
        }

        public void OnMove_C(InputAction.CallbackContext context) {
            MoveEvent?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnLook_C(InputAction.CallbackContext context) { }

        public void OnZoom_C(InputAction.CallbackContext context) { }

        public void OnPan_C(InputAction.CallbackContext context) {
            PanEvent.Invoke(context.ReadValueAsButton());
        }

        public void OnTurbo_C(InputAction.CallbackContext context) {
            TurboEvent.Invoke(context.performed);
        }

        public void OnClick_C(InputAction.CallbackContext context) {
            bool isPressed = context.ReadValueAsButton();

            // When Submit activates a focused ClusterList button, let UI own the action and
            // suppress world click sampling so selected ID doesn't get immediately overwritten.
            if (isPressed && IsSubmitFocusedOnObjectList(context))
                return;

            ClickEvent.Invoke(isPressed);
        }

        public void OnSwapRenderMode_C(InputAction.CallbackContext context) {
            SwapRenderModeEvent.Invoke(context.performed);
        }

        #endregion

        #region Animation Player

        //Prefix for later use in UI segmentation
        public void OnPlayAndPause_A(InputAction.CallbackContext context) {
            PlayEvent?.Invoke(context.performed);
        }

        public void OnStepForward_A(InputAction.CallbackContext context) {
            StepForwardEvent?.Invoke(context.performed);
        }

        public void OnStepBackward_A(InputAction.CallbackContext context) {
            StepBackwardEvent?.Invoke(context.performed);
        }

        public void OnPlayAndPause(InputAction.CallbackContext context) {
            PlayEvent?.Invoke(context.performed);
        }

        public void OnStepForward(InputAction.CallbackContext context) {
            StepForwardEvent?.Invoke(context.performed);
        }

        public void OnStepBackward(InputAction.CallbackContext context) {
            StepBackwardEvent?.Invoke(context.performed);
        }

        #endregion

        public void OnSettings(InputAction.CallbackContext context) {
            if (context.performed)
                SettingsEvent.Invoke();
        }

        #endregion
    }
}