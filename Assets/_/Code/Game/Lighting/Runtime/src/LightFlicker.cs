using Core.Runtime;
using UnityEngine;

namespace Lighting.Runtime
{
    /// <summary>
    /// Generic, fully tunable flicker for any environment light (bedside lamps,
    /// corridor tubes, ...). Flicker can be turned off entirely to leave a steady
    /// light, or driven with a random cadence: at random intervals it jumps the
    /// intensity to a new random value inside a configured range, optionally eased.
    /// Drop it on any Light and set it to taste per instance. Ticked by the
    /// <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class LightFlicker : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private Light _light;
        [Tooltip("Off = the light stays perfectly steady at base intensity.")]
        [SerializeField] private bool _canFlicker = true;
        [SerializeField] private float _baseIntensity = 1.0f;

        [Header("Flicker Intensity Range")]
        [SerializeField] private float _minIntensity = 0.2f;
        [SerializeField] private float _maxIntensity = 1.2f;

        [Header("Random Cadence (s between changes)")]
        [SerializeField] private float _minInterval = 0.04f;
        [SerializeField] private float _maxInterval = 0.35f;

        [Header("Smoothing")]
        [Tooltip("Off = hard, snappy flicker. On = eased transitions.")]
        [SerializeField] private bool _smooth;
        [SerializeField] private float _smoothSpeed = 20.0f;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (_light == null) return;

            if (!_canFlicker)
            {
                _light.intensity = _baseIntensity;
                return;
            }

            _timer -= deltaTime;
            if (_timer <= 0.0f)
            {
                _targetIntensity = Random.Range(_minIntensity, _maxIntensity);
                _timer = Random.Range(_minInterval, _maxInterval);
            }

            _light.intensity = _smooth
                ? Mathf.MoveTowards(_light.intensity, _targetIntensity, _smoothSpeed * deltaTime)
                : _targetIntensity;
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _targetIntensity = _baseIntensity;
            _timer = 0.0f;

            _updateManager.Register(this);
        }

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private float _timer;
        private float _targetIntensity;

        #endregion
    }
}