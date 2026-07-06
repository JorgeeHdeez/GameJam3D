using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// Converts the player's state into a stealth noise radius and broadcasts it
    /// through a ScriptableObject event channel. Listeners (e.g. enemy perception)
    /// react without holding any direct reference to the player.
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
        /// Computes the noise radius for <paramref name="state"/> and raises the
        /// channel only when the value actually changes, to avoid spamming listeners.
        /// </summary>
        public void Emit(PlayerState state)
        {
            float radius = ResolveRadius(state);
            if (Mathf.Approximately(radius, _currentNoiseRadius)) return;

            _currentNoiseRadius = radius;

            if (_noiseChannel == null) return;
            _noiseChannel.Raise(radius);
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