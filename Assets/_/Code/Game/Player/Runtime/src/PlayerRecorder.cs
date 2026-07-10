using System.Collections.Generic;
using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Records the player's view pose every tick while the game is in the Playing
    /// state, building the timeline the rewind then plays back in reverse. Recording
    /// pauses automatically outside Playing (during a rewind or after winning) since
    /// the state gate stops it. Receives its tick from the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class PlayerRecorder : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private Transform _playerBody;
        [SerializeField] private Transform _cameraPivot;
        [Tooltip("Safety cap so an idle run cannot grow the buffer without bound.")]
        [SerializeField] private int _maxRecordedFrames = 20000;

        #endregion


        #region Properties

        /// <summary>Number of frames currently recorded.</summary>
        public int Count => _snapshots.Count;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (_gameState == null || _gameState.CurrentStatePlayer != GameStatePlayer.Playing) return;
            if (_playerBody == null || _cameraPivot == null) return;
            if (_snapshots.Count >= _maxRecordedFrames) return;

            _snapshots.Add(new PlayerSnapshot(
                _playerBody.position,
                _playerBody.rotation,
                _cameraPivot.localPosition,
                _cameraPivot.localRotation));
        }

        public PlayerSnapshot Get(int index) => _snapshots[index];

        public void Clear() => _snapshots.Clear();

        #endregion


        #region Unity Callbacks

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private readonly List<PlayerSnapshot> _snapshots = new();

        #endregion
    }
}
