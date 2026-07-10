using Core.Runtime;
using UnityEngine;

namespace Lighting.Runtime
{
    /// <summary>
    /// When something on the configured layers enters its trigger, the light surges to
    /// a maximum intensity over a short charge, then bursts: a particle system fires and
    /// the light is cut. A deterministic timed ramp (no coroutine) drives the surge.
    /// Fires once. Needs a trigger collider. Ticked by the <see cref="UpdateManager"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class LightExplosion : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private Light _light;
        [SerializeField] private ParticleSystem _explosionParticles;
        [SerializeField] private NoisePingEventChannelSO _noisePingChannel;
        [SerializeField] private float _noiseRadius = 15.0f;
        [SerializeField] private LayerMask _triggerLayers;
        [SerializeField] private float _maxIntensity = 8.0f;
        [SerializeField] private float _chargeDuration = 1.0f;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (_phase != Phase.Charging) return;

            _chargeTimer += deltaTime;
            float t = _chargeDuration <= 0.0f ? 1.0f : Mathf.Clamp01(_chargeTimer / _chargeDuration);

            if (_light != null) _light.intensity = Mathf.Lerp(_startIntensity, _maxIntensity, t);

            if (t >= 1.0f) Explode();
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        private void OnTriggerEnter(Collider other)
        {
            if (_phase != Phase.Idle) return;
            if ((_triggerLayers.value & (1 << other.gameObject.layer)) == 0) return;

            _startIntensity = _light != null ? _light.intensity : 0.0f;
            _chargeTimer = 0.0f;
            _phase = Phase.Charging;
        }

        #endregion


        #region Private Fields

        private Phase _phase = Phase.Idle;
        private float _chargeTimer;
        private float _startIntensity;

        #endregion


        #region Private Methods

        private void Explode()
        {
            _phase = Phase.Exploded;

            if (_explosionParticles != null) _explosionParticles.Play();

            // The bang is a loud event: ping enemies so they investigate the spot.
            if (_noisePingChannel != null) _noisePingChannel.Raise(new NoisePing(transform.position, _noiseRadius));

            if (_light == null) return;
            _light.intensity = 0.0f;
            _light.enabled = false;
        }

        private enum Phase
        {
            Idle,
            Charging,
            Exploded
        }

        #endregion
    }
}