using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Turns how close the most aware enemy is to spotting the player into a single
    /// smoothed 0..1 stress value and broadcasts it on a float channel, so decoupled
    /// systems (heartbeat audio, screen vignette, a UI gauge) can react without ever
    /// referencing an enemy. Reads the shared detection registry instead of holding
    /// enemy references, keeping the Player assembly independent of the Enemy one.
    /// Stress rises fast and eases off slowly, so the tension lingers after a scare.
    /// Receives its tick from the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class PlayerStress : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private DetectionRegistrySO _detectionRegistry;
        [SerializeField] private FloatEventChannelSO _stressChannel;
        [SerializeField] private float _riseSpeed = 3.0f;
        [SerializeField] private float _fallSpeed = 0.8f;

        #endregion


        #region Properties

        /// <summary>Current smoothed stress level, 0 (calm) to 1 (about to be caught).</summary>
        public float Stress => _stress;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            float target = _detectionRegistry != null ? _detectionRegistry.MaxDetectionLevel : 0.0f;
            float speed = target > _stress ? _riseSpeed : _fallSpeed;

            _stress = Mathf.MoveTowards(_stress, target, speed * deltaTime);

            if (Mathf.Approximately(_stress, _lastBroadcast)) return;

            _lastBroadcast = _stress;

            if (_stressChannel == null) return;
            _stressChannel.Raise(_stress);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private float _stress;
        private float _lastBroadcast = -1.0f;

        #endregion
    }
}
