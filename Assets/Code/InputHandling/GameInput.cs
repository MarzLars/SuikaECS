using UnityEngine.InputSystem;

namespace Code.InputHandling
{
    public static class GameInput
    {
        static bool _initialized;

        static InputActionMap _defaultMap;
        static InputActionMap _playerMap;

        public static InputAction Move;
        public static InputAction Look;
        public static InputAction Interact;
        public static InputAction ShowInventory;
        public static InputAction Cancel;
        public static InputAction Click;
        public static InputAction Submit;

        public static void Initialize()
        {
            if (_initialized)
                return;

            var actionsAsset = InputSystem.actions;
            if (actionsAsset == null)
                return;

            _defaultMap = actionsAsset.FindActionMap("Default", false);
            _playerMap = actionsAsset.FindActionMap("Player", false);

            if (_playerMap != null)
            {
                Move = _playerMap.FindAction("Move", false);
                Look = _playerMap.FindAction("Look", false);
                Interact = _playerMap.FindAction("Interact", false);
                ShowInventory = _playerMap.FindAction("ShowInventory", false);
                Cancel = _playerMap.FindAction("Cancel", false);
                _playerMap.Enable();
            }

            if (_defaultMap != null)
            {
                Click = _defaultMap.FindAction("Click", false);
                Submit = _defaultMap.FindAction("Submit", false);
                _defaultMap.Enable();
            }

            _initialized = true;
        }

        public static bool DropPressedThisFrame()
        {
            if (!_initialized)
                Initialize();

            bool interactPressed = Interact != null && Interact.WasPressedThisFrame();
            bool clickPressed = Click != null && Click.WasPressedThisFrame();
            bool submitPressed = Submit != null && Submit.WasPressedThisFrame();

            // Fail-safe fallback for setups where InputSystem.actions is not configured.
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool keyboardPressed = Keyboard.current != null &&
                                   (Keyboard.current.spaceKey.wasPressedThisFrame ||
                                    Keyboard.current.enterKey.wasPressedThisFrame);

            return interactPressed || clickPressed || submitPressed || mousePressed || keyboardPressed;
        }
    }
}