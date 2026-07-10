using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Turns being caught into the game's signature rewind: on the catch event it
    /// suspends player control and replays the recorded timeline backwards at high
    /// speed until the player is back at the start pose, then clears the timeline and
    /// returns to Playing. Enemies react to that state change on their own (freeze
    /// during the rewind, reset when Playing resumes), so this controller never
    /// references them. Receives its tick from the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class RewindController : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private PlayerRecorder _recorder;
        [SerializeField] private VoidEventChannelSO _caughtChannel;

        [Header("Suspended During Rewind")]
        [SerializeField] private Transform _playerBody;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerLook _playerLook;
        [SerializeField] private PlayerCameraRig _cameraRig;
        [SerializeField] private PlayerHideController _hideController;

        [Header("Rewind")]
        [Tooltip("Recorded frames consumed per second - higher rewinds faster.")]
        [SerializeField] private float _rewindFramesPerSecond = 300.0f;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (_gameState == null || _gameState.CurrentStatePlayer != GameStatePlayer.Rewinding) return;

            StepRewind(deltaTime);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _updateManager.Register(this);

            if (_caughtChannel != null) _caughtChannel.OnEventRaised += OnCaught;
        }

        private void OnDisable()
        {
            _updateManager.Unregister(this);

            if (_caughtChannel != null) _caughtChannel.OnEventRaised -= OnCaught;
        }

        #endregion


        #region Private Fields

        private float _cursor;

        #endregion


        #region Private Methods

        private void OnCaught()
        {
            if (_gameState == null || _gameState.CurrentStatePlayer != GameStatePlayer.Playing) return;
            if (_recorder == null || _recorder.Count == 0) return;

            SetPlayerControlEnabled(false);
            _cursor = _recorder.Count - 1;
            _gameState.SetState(GameStatePlayer.Rewinding);
        }

        private void StepRewind(float deltaTime)
        {
            _cursor -= _rewindFramesPerSecond * deltaTime;

            if (_cursor <= 0.0f)
            {
                ApplySnapshot(_recorder.Get(0));
                CompleteRewind();
                return;
            }

            int index = Mathf.Clamp(Mathf.RoundToInt(_cursor), 0, _recorder.Count - 1);
            ApplySnapshot(_recorder.Get(index));
        }

        private void ApplySnapshot(PlayerSnapshot snapshot)
        {
            if (_playerBody != null) _playerBody.SetPositionAndRotation(snapshot.BodyPosition, snapshot.BodyRotation);

            if (_cameraPivot == null) return;
            _cameraPivot.localPosition = snapshot.PivotLocalPosition;
            _cameraPivot.localRotation = snapshot.PivotLocalRotation;
        }

        private void CompleteRewind()
        {
            _recorder.Clear();

            // Hand the view back to free-look aligned with the start pose, so control
            // resumes without a jump.
            if (_playerLook != null && _cameraPivot != null) _playerLook.AdoptLook(_cameraPivot.rotation);

            SetPlayerControlEnabled(true);

            // Returning to Playing is what tells enemies to reset themselves.
            _gameState.SetState(GameStatePlayer.Playing);
        }

        private void SetPlayerControlEnabled(bool isEnabled)
        {
            if (_playerController != null) _playerController.enabled = isEnabled;
            if (_playerLook != null) _playerLook.enabled = isEnabled;
            if (_cameraRig != null) _cameraRig.enabled = isEnabled;
            if (_hideController != null) _hideController.enabled = isEnabled;
        }

        #endregion
    }
}
