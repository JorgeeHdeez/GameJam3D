using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Mouse and gamepad look: yaws the player body left/right and pitches the
    /// camera pivot up/down. Mouse delta already represents the movement since the
    /// last frame, so it only needs a flat sensitivity scale; the gamepad stick is a
    /// held direction representing an angular rate, so it is additionally scaled by
    /// deltaTime to stay framerate-independent. Yaw rotates the player's own
    /// transform (so <see cref="PlayerController"/>'s body-relative movement turns
    /// with it), pitch only ever touches the camera pivot. Receives its tick from
    /// the <see cref="UpdateManager"/>; register it to tick before
    /// <see cref="PlayerController"/> so movement uses the current frame's yaw
    /// instead of lagging one frame behind.
    /// </summary>
    public sealed class PlayerLook : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private Transform _cameraPivot;

        [Header("Sensitivity")]
        [SerializeField] private float _mouseSensitivity = 0.15f;
        [SerializeField] private float _gamepadSensitivity = 120.0f;

        [Header("Pitch Limits (degrees)")]
        [SerializeField] private float _minPitch = -80.0f;
        [SerializeField] private float _maxPitch = 80.0f;

        [SerializeField] private bool _invertY;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            // In cover the camera rig owns the pivot rotation (wall-aligned framing),
            // so free-look yields to avoid two systems fighting over it. Leaving the
            // body yaw and stored pitch untouched here lets free-look resume seamlessly
            // the moment cover is released.
            if (_playerController != null && _playerController.IsInCover) return;

            Vector2 look = ResolveLookDelta(deltaTime);
            if (look.sqrMagnitude < LookThresholdSqr) return;

            transform.Rotate(Vector3.up, look.x, Space.World);
            ApplyPitch(look.y);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private float _pitch;

        #endregion


        #region Private Constants

        private const float LookThresholdSqr = 0.0000001f;

        #endregion


        #region Private Methods

        // Mouse delta is already a per-frame movement, so it only needs a flat
        // sensitivity scale. The gamepad stick is a held direction, so it is
        // additionally scaled by deltaTime to give a stable turn rate regardless
        // of framerate.
        private Vector2 ResolveLookDelta(float deltaTime)
        {
            Vector2 mouseDelta = _input.LookMouseDelta * _mouseSensitivity;
            Vector2 stickDelta = _input.LookStick * (_gamepadSensitivity * deltaTime);

            return mouseDelta + stickDelta;
        }

        private void ApplyPitch(float delta)
        {
            float signedDelta = _invertY ? delta : -delta;

            _pitch = Mathf.Clamp(_pitch + signedDelta, _minPitch, _maxPitch);

            if (_cameraPivot == null) return;
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0.0f, 0.0f);
        }

        #endregion
    }
}