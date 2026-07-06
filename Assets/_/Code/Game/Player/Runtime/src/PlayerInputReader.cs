using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Runtime
{
    /// <summary>
    /// Wraps the New Input System actions used by the player and exposes the
    /// current input state through simple read-only members. Actions are injected
    /// as <see cref="InputActionReference"/> and wired in the Inspector, so the
    /// reader never resolves anything at runtime.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        #region Fields

        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _runAction;
        [SerializeField] private InputActionReference _crouchAction;
        [SerializeField] private InputActionReference _crawlAction;
        [SerializeField] private InputActionReference _wallHugAction;
        [SerializeField] private InputActionReference _peekAction;
        [SerializeField] private InputActionReference _lookMouseAction;
        [SerializeField] private InputActionReference _lookStickAction;

        #endregion


        #region Properties

        /// <summary>Raw 2D movement input, range [-1, 1] per axis.</summary>
        public Vector2 MoveInput => _moveAction.action.ReadValue<Vector2>();

        /// <summary>True while the run modifier is held down.</summary>
        public bool RunHeld => _runAction.action.IsPressed();

        /// <summary>True while the wall-hug modifier is held down.</summary>
        public bool WallHugHeld => _wallHugAction.action.IsPressed();

        /// <summary>
        /// True on the single frame the cover button is pressed. Used as an explicit
        /// enter/exit toggle for cover in first person, rather than the automatic
        /// wall-push detection used by the third-person controller.
        /// </summary>
        public bool WallHugPressed => _wallHugAction.action.WasPressedThisFrame();

        /// <summary>True while the corner-peek modifier is held down.</summary>
        public bool PeekHeld => _peekAction.action.IsPressed();

        /// <summary>
        /// Raw mouse delta since last frame (pixels), already frame-independent.
        /// Bind this action to Mouse/delta.
        /// </summary>
        public Vector2 LookMouseDelta => _lookMouseAction.action.ReadValue<Vector2>();

        /// <summary>
        /// Raw gamepad look stick value, range [-1, 1] per axis. This is a held
        /// direction, not a delta, so it must be scaled by deltaTime by the reader.
        /// Bind this action to Gamepad/rightStick.
        /// </summary>
        public Vector2 LookStick => _lookStickAction.action.ReadValue<Vector2>();

        /// <summary>True while the posture intent is prone (crawling).</summary>
        public bool CrawlActive => _posture == Posture.Prone;

        /// <summary>True while the posture intent is crouched.</summary>
        public bool CrouchActive => _posture == Posture.Crouch;

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _runAction.action.Enable();
            _crouchAction.action.Enable();
            _crawlAction.action.Enable();
            _wallHugAction.action.Enable();
            _peekAction.action.Enable();
            _lookMouseAction.action.Enable();
            _lookStickAction.action.Enable();

            _crouchAction.action.performed += OnCrouchPerformed;
            _crawlAction.action.performed += OnCrawlPerformed;
        }

        private void OnDisable()
        {
            _crouchAction.action.performed -= OnCrouchPerformed;
            _crawlAction.action.performed -= OnCrawlPerformed;

            _moveAction.action.Disable();
            _runAction.action.Disable();
            _crouchAction.action.Disable();
            _crawlAction.action.Disable();
            _wallHugAction.action.Disable();
            _peekAction.action.Disable();
            _lookMouseAction.action.Disable();
            _lookStickAction.action.Disable();
        }

        #endregion


        #region Private Fields

        private Posture _posture = Posture.Stand;

        #endregion


        #region Private Methods

        // Posture ladder (MGS-style), so every posture is reachable from every other:
        // - Crouch button: Stand <-> Crouch, and stands the character up to Crouch from Prone.
        // - Crawl button: any posture -> Prone, and Prone -> Stand.
        private void OnCrouchPerformed(InputAction.CallbackContext context) =>
            _posture = _posture == Posture.Crouch ? Posture.Stand : Posture.Crouch;

        private void OnCrawlPerformed(InputAction.CallbackContext context) =>
            _posture = _posture == Posture.Prone ? Posture.Stand : Posture.Prone;

        private enum Posture
        {
            Stand,
            Crouch,
            Prone
        }

        #endregion
    }
}