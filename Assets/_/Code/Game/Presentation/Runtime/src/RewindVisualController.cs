using Core.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace Presentation.Runtime
{
    /// <summary>
    /// Ramps a post-processing Volume in and out with the rewind: its weight eases
    /// toward 1 while the game is Rewinding and back to 0 otherwise, so the profile's
    /// distortion / aberration / grain effects fade in for the eerie retrace and fade
    /// out cleanly when play resumes. Only drives the weight - the look itself lives in
    /// the Volume profile. Receives its tick from the <see cref="UpdateManager"/>.
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public sealed class RewindVisualController : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private float _transitionSpeed = 5.0f;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (_volume == null || _gameState == null) return;

            float targetWeight = _gameState.CurrentState == GameState.Rewinding ? 1.0f : 0.0f;
            if (Mathf.Approximately(_volume.weight, targetWeight)) return;

            _volume.weight = Mathf.MoveTowards(_volume.weight, targetWeight, _transitionSpeed * deltaTime);
        }

        #endregion


        #region Unity Callbacks

        private void Awake()
        {
            _volume = GetComponent<Volume>();
            _volume.weight = 0.0f;
        }

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private Volume _volume;

        #endregion
    }
}
