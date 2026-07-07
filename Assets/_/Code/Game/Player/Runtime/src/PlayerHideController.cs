using Core.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Runtime
{
    /// <summary>
    /// Lets the player tuck into a nearby <see cref="HideSpot"/> and back out again,
    /// as an explicit button toggle offered only while standing in a spot. While
    /// hidden the player is undetectable (via the shared visibility flag), cannot move
    /// or look (the movement, look and camera-rig behaviours are suspended so nothing
    /// fights the framing), and the camera is snapped to the spot's anchor. Suspending
    /// those behaviours simply disables them, which unregisters them from the update
    /// loop until the player leaves the spot.
    /// </summary>
    public sealed class PlayerHideController : MonoBehaviour
    {
        #region Fields

        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerLook _playerLook;
        [SerializeField] private PlayerCameraRig _cameraRig;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private PlayerVisibilitySO _playerVisibility;
        [SerializeField] private InputActionReference _hideAction;

        #endregion


        #region Properties

        /// <summary>True while the player is hidden inside a spot.</summary>
        public bool IsHidden => _isHidden;

        /// <summary>
        /// True while a hide spot is in reach and the player is not yet hidden.
        /// Drive a "hide" input prompt from this.
        /// </summary>
        public bool CanHide => _availableSpot != null && !_isHidden;

        #endregion


        #region Public API

        public void SetAvailableSpot(HideSpot spot) => _availableSpot = spot;

        public void ClearAvailableSpot(HideSpot spot)
        {
            if (_availableSpot == spot) _availableSpot = null;
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            // Clear any stale hidden flag left on the asset from a previous play session.
            if (_playerVisibility != null) _playerVisibility.SetHidden(false);

            if (_hideAction == null) return;

            _hideAction.action.Enable();
            _hideAction.action.performed += OnHidePerformed;
        }

        private void OnDisable()
        {
            if (_hideAction == null) return;

            _hideAction.action.performed -= OnHidePerformed;
            _hideAction.action.Disable();
        }

        #endregion


        #region Private Fields

        private HideSpot _availableSpot;
        private bool _isHidden;

        #endregion


        #region Private Methods

        private void OnHidePerformed(InputAction.CallbackContext context)
        {
            if (_isHidden)
            {
                Exit();
                return;
            }

            if (_availableSpot == null) return;

            Enter(_availableSpot);
        }

        private void Enter(HideSpot spot)
        {
            _isHidden = true;
            if (_playerVisibility != null) _playerVisibility.SetHidden(true);

            SetPlayerBehavioursEnabled(false);

            if (_cameraPivot == null || spot.CameraAnchor == null) return;
            _cameraPivot.SetPositionAndRotation(spot.CameraAnchor.position, spot.CameraAnchor.rotation);
        }

        private void Exit()
        {
            _isHidden = false;
            if (_playerVisibility != null) _playerVisibility.SetHidden(false);

            // Re-enabling the camera rig restores the pivot to its normal framing on
            // the next tick, so no manual reset is needed here.
            SetPlayerBehavioursEnabled(true);
        }

        private void SetPlayerBehavioursEnabled(bool isEnabled)
        {
            if (_playerController != null) _playerController.enabled = isEnabled;
            if (_playerLook != null) _playerLook.enabled = isEnabled;
            if (_cameraRig != null) _cameraRig.enabled = isEnabled;
        }

        #endregion
    }
}
