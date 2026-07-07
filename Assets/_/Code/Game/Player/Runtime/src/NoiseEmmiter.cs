using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Converts the player's state into a stealth noise radius and broadcasts it
    /// through a ScriptableObject event channel. Listeners (e.g. enemy perception)
    /// react without holding any direct reference to the player. The radius is
    /// broadcast every tick rather than only on change: event channels do not replay
    /// their last value to new or reset subscribers, so re-broadcasting guarantees an
    /// enemy that subscribes late or has its hearing reset (e.g. after a rewind) always
    /// has the current radius instead of being stuck deaf until the next change.
    /// </summary>
    public sealed class NoiseEmitter : MonoBehaviour
    {
        #region Fields

        [SerializeField] private FloatEventChannelSO _noiseChannel;
        [SerializeField] private float _walkNoiseRadius = 4.0f;
        [SerializeField] private float _runNoiseRadius = 9.0f;
        [SerializeField] private float _crouchNoiseRadius = 2.0f;
        [SerializeField] private float _crawlNoiseRadius = 1.0f;

        #endregion


        #region Properties

        /// <summary>Last noise radius broadcast by this emitter.</summary>
        public float CurrentNoiseRadius => _currentNoiseRadius;

        #endregion


        #region Public API

        /// <summary>
        /// Computes the noise radius for <paramref name="state"/> and broadcasts it on
        /// the channel every call, so listeners always hold the current value.
        /// </summary>
        public void Emit(PlayerState state)
        {
            _currentNoiseRadius = ResolveRadius(state);

            if (_noiseChannel == null) return;
            _noiseChannel.Raise(_currentNoiseRadius);
        }

        #endregion


        #region Unity Callbacks

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _currentNoiseRadius);
        }

        #endregion


        #region Private Fields

        private float _currentNoiseRadius;

        #endregion


        #region Private Methods

        private float ResolveRadius(PlayerState state) => state switch
        {
            PlayerState.Walking => _walkNoiseRadius,
            PlayerState.Running => _runNoiseRadius,
            PlayerState.Crouching => _crouchNoiseRadius,
            PlayerState.Crawling => _crawlNoiseRadius,
            _ => 0.0f
        };

        #endregion
    }
}