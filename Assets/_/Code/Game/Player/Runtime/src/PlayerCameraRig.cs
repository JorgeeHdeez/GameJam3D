using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Sole owner of the first-person camera pivot. Composes the pivot's local
    /// position every frame (rest pose + crouch/crawl height drop + cover glue +
    /// corner-peek lean) and, while in cover, also drives its look direction.
    ///
    /// Rotation ownership is split by mode to avoid two systems writing it: out of
    /// cover, <see cref="PlayerLook"/> owns pitch (and the body owns yaw); in cover,
    /// PlayerLook yields and this rig frames the view along the wall. In cover the
    /// look input becomes a clamped free-look cone (a few dozen degrees off the wall
    /// direction) instead of full 360 freedom, so the player can glance out without
    /// losing the wall framing. Corner peek leans the camera past an outer corner and
    /// turns it slightly to reveal what is around it. Receives its tick from the
    /// <see cref="UpdateManager"/>; register it AFTER <see cref="PlayerController"/>
    /// so it reads the current frame's cover state.
    /// </summary>
    public sealed class PlayerCameraRig : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private PlayerInputReader _input;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerLook _playerLook;
        [SerializeField] private CornerSensor _cornerSensor;
        [SerializeField] private Transform _playerBody;
        [SerializeField] private Transform _cameraPivot;

        [Header("Posture Height Drop (m)")]
        [SerializeField] private float _crouchHeightDrop = 0.6f;
        [SerializeField] private float _crawlHeightDrop = 1.2f;
        [SerializeField] private float _heightLerpSpeed = 6.0f;

        [Header("Cover Framing")]
        [Tooltip("How far the camera is pushed toward the wall while in cover.")]
        [SerializeField] private float _coverGlueDistance = 0.35f;
        [Tooltip("Half-angle (deg) of the free-look cone allowed off the wall direction in cover.")]
        [SerializeField] private float _coverYawClamp = 35.0f;
        [SerializeField] private float _coverPitchClamp = 45.0f;
        [SerializeField] private float _coverMouseSensitivity = 0.12f;
        [SerializeField] private float _coverGamepadSensitivity = 90.0f;

        [Header("Corner Peek")]
        [SerializeField] private float _peekSpeed = 5.0f;
        [SerializeField] private float _peekSideShift = 0.45f;
        [SerializeField] private float _peekOutwardShift = 0.25f;
        [Tooltip("Extra yaw (deg) applied at full peek to look around the corner. " +
                 "Flip the sign if it turns the wrong way.")]
        [SerializeField] private float _peekYaw = 25.0f;

        #endregion


        #region Properties

        /// <summary>Current corner-peek blend, 0 (tucked in) to 1 (fully leaning out).</summary>
        public float PeekAmount => _peekAmount;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (_cameraPivot == null) return;

            bool inCover = _playerController != null && _playerController.IsInCover;

            if (inCover != _wasInCover)
            {
                if (inCover) EnterCover();
                else ExitCover();

                _wasInCover = inCover;
            }

            if (inCover)
            {
                UpdateCover(deltaTime);
                return;
            }

            UpdateFreeHeight(deltaTime);
        }

        #endregion


        #region Unity Callbacks

        private void Awake()
        {
            if (_cameraPivot != null) _pivotRestLocalPosition = _cameraPivot.localPosition;
        }

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private Vector3 _pivotRestLocalPosition;
        private float _currentHeightDrop;
        private float _peekAmount;
        private float _coverYaw;
        private float _coverPitch;
        private bool _wasInCover;

        #endregion


        #region Private Constants

        private const float DirectionEpsilon = 0.0001f;

        #endregion


        #region Private Methods

        // Out of cover the rig only owns the pivot position (height); PlayerLook owns
        // rotation. The camera eases down for crouch/crawl and back up otherwise.
        private void UpdateFreeHeight(float deltaTime)
        {
            float targetDrop = ResolveHeightDrop();
            _currentHeightDrop = Mathf.MoveTowards(_currentHeightDrop, targetDrop, _heightLerpSpeed * deltaTime);

            _cameraPivot.localPosition = _pivotRestLocalPosition + Vector3.down * _currentHeightDrop;
        }

        private float ResolveHeightDrop() => _playerController == null ? 0.0f : _playerController.CurrentState switch
        {
            PlayerState.Crawling => _crawlHeightDrop,
            PlayerState.Crouching => _crouchHeightDrop,
            _ => 0.0f
        };

        // Start every cover session looking straight along the wall, tucked in.
        private void EnterCover()
        {
            _coverYaw = 0.0f;
            _coverPitch = 0.0f;
            _peekAmount = 0.0f;
            _currentHeightDrop = 0.0f;
        }

        // Hand the current wall-aligned orientation back to free-look so releasing
        // cover does not snap the view, and - crucially - so the body-relative
        // movement axes match what the player is looking at. Without this, flipping
        // the sneak direction in cover and then exiting would invert the controls,
        // because the body yaw would still point at the (stale) cover-entry direction.
        private void ExitCover()
        {
            if (_playerLook != null) _playerLook.AdoptLook(_cameraPivot.rotation);
        }

        private void UpdateCover(float deltaTime)
        {
            Vector3 wallNormal = _playerController.CoverWallNormal;
            if (wallNormal.sqrMagnitude < DirectionEpsilon) return;

            bool facingRight = _playerController.CoverFacingRight;
            Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal);
            Vector3 travel = facingRight ? wallRight : -wallRight;
            float facingSign = facingRight ? 1.0f : -1.0f;

            UpdatePeek(facingRight, wallNormal, deltaTime);
            AccumulateCoverLook(deltaTime);

            ApplyCoverPosition(wallNormal, travel, facingSign);
            ApplyCoverRotation(travel, facingSign);
        }

        // Peek ramps in while the player holds the peek button and an outer corner
        // exists on the side they are facing.
        private void UpdatePeek(bool facingRight, Vector3 wallNormal, float deltaTime)
        {
            if (_cornerSensor != null && _playerBody != null) _cornerSensor.Check(_playerBody, wallNormal);

            bool cornerAvailable = _cornerSensor != null &&
                (facingRight ? _cornerSensor.HasRightCorner : _cornerSensor.HasLeftCorner);
            bool wantsPeek = cornerAvailable && _input.PeekHeld;

            _peekAmount = Mathf.MoveTowards(_peekAmount, wantsPeek ? 1.0f : 0.0f, _peekSpeed * deltaTime);
        }

        // In cover the look input feeds a clamped cone instead of free 360 rotation:
        // mouse delta is already per-frame, the stick is scaled by deltaTime.
        private void AccumulateCoverLook(float deltaTime)
        {
            Vector2 look = _input.LookMouseDelta * _coverMouseSensitivity
                         + _input.LookStick * (_coverGamepadSensitivity * deltaTime);

            _coverYaw = Mathf.Clamp(_coverYaw + look.x, -_coverYawClamp, _coverYawClamp);
            _coverPitch = Mathf.Clamp(_coverPitch - look.y, -_coverPitchClamp, _coverPitchClamp);
        }

        private void ApplyCoverPosition(Vector3 wallNormal, Vector3 travel, float facingSign)
        {
            Vector3 worldOffset = -wallNormal * _coverGlueDistance
                                + travel * (_peekSideShift * _peekAmount)
                                + wallNormal * (_peekOutwardShift * _peekAmount);

            Vector3 localOffset = _playerBody != null ? _playerBody.InverseTransformVector(worldOffset) : worldOffset;
            _cameraPivot.localPosition = _pivotRestLocalPosition + localOffset;
        }

        // Base look runs along the wall in the travel direction; the clamped free-look
        // cone and the peek yaw are layered on top as a world-space rotation, since
        // PlayerLook is suppressed while in cover.
        private void ApplyCoverRotation(Vector3 travel, float facingSign)
        {
            Quaternion baseLook = Quaternion.LookRotation(travel, Vector3.up);
            float yaw = _coverYaw + facingSign * _peekYaw * _peekAmount;

            _cameraPivot.rotation = baseLook * Quaternion.Euler(_coverPitch, yaw, 0.0f);
        }

        #endregion
    }
}