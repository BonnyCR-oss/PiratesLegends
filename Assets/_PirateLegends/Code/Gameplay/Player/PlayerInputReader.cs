using UnityEngine;
using UnityEngine.InputSystem;

namespace PirateLegends.Player
{
    [DefaultExecutionOrder(-200)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private InputActionMap actions;
        private InputAction move, look, jump, sprint, releaseCursor, captureCursor;
        private float jumpPressedAt = float.NegativeInfinity;
        public Vector2 Move { get; private set; }
        public Vector2 Look { get; private set; }
        public bool Sprint { get; private set; }

        private void Awake()
        {
            actions = new InputActionMap("LocalPlayer");
            move = actions.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            look = actions.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
            jump = actions.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            sprint = actions.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
            sprint.AddBinding("<Keyboard>/rightShift");
            releaseCursor = actions.AddAction("ReleaseCursor", InputActionType.Button, "<Keyboard>/escape");
            captureCursor = actions.AddAction("CaptureCursor", InputActionType.Button, "<Mouse>/leftButton");
        }

        private void OnEnable() { actions.Enable(); SetCursor(true); }
        private void OnDisable()
        {
            actions.Disable(); Move = Look = Vector2.zero; Sprint = false;
            jumpPressedAt = float.NegativeInfinity; SetCursor(false);
        }
        private void OnDestroy() { actions?.Dispose(); }
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) { SetCursor(false); jumpPressedAt = float.NegativeInfinity; }
        }
        private static void SetCursor(bool capture)
        {
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }
        private void Update()
        {
            if (releaseCursor.WasPressedThisFrame()) SetCursor(false);
            else if (captureCursor.WasPressedThisFrame()) SetCursor(true);
            bool active = Cursor.lockState == CursorLockMode.Locked;
            Move = active ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            Look = active ? look.ReadValue<Vector2>() : Vector2.zero;
            Sprint = active && sprint.IsPressed();
            if (active && jump.WasPressedThisFrame()) jumpPressedAt = Time.time;
        }
        public bool ConsumeJump(float bufferSeconds)
        {
            if (Time.time - jumpPressedAt > bufferSeconds) return false;
            jumpPressedAt = float.NegativeInfinity;
            return true;
        }
    }
}
