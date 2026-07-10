using Core.Runtime;
using UnityEngine;

namespace Lighting.Runtime
{
    /// <summary>
    /// Horror-film flicker for the player's flashlight spot light: the beam stays
    /// mostly steady, then at random intervals stutters in a short burst of rapid dips
    /// before recovering - a failing-torch feel, rather than the constantly erratic
    /// flicker of a broken fixture. Place it on the flashlight Light (under the camera
    /// pivot). Ticked by the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class FlashlightFlicker : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private Light _light;
        [SerializeField] private float _baseIntensity = 2.0f;

        [Header("Random Calm Between Bursts (s)")]
        [SerializeField] private float _minCalm = 2.5f;
        [SerializeField] private float _maxCalm = 9.0f;

        [Header("Stutter Burst")]
        [SerializeField] private float _burstDuration = 0.45f;
        [SerializeField] private float _dipMinIntensity = 0.0f;
        [SerializeField] private float _stutterMinInterval = 0.02f;
        [SerializeField] private float _stutterMaxInterval = 0.09f;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (_light == null) return;

            if (_isBursting) TickBurst(deltaTime);
            else TickCalm(deltaTime);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _light.intensity = _baseIntensity;
            _calmTimer = Random.Range(_minCalm, _maxCalm);
            _isBursting = false;

            _updateManager.Register(this);
        }

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private bool _isBursting;
        private float _calmTimer;
        private float _burstTimer;
        private float _stutterTimer;

        #endregion


        #region Private Methods

        private void TickCalm(float deltaTime)
        {
            _light.intensity = _baseIntensity;

            _calmTimer -= deltaTime;
            if (_calmTimer > 0.0f) return;

            _isBursting = true;
            _burstTimer = _burstDuration;
            _stutterTimer = 0.0f;
        }

        private void TickBurst(float deltaTime)
        {
            _stutterTimer -= deltaTime;
            if (_stutterTimer <= 0.0f)
            {
                _light.intensity = Random.Range(_dipMinIntensity, _baseIntensity);
                _stutterTimer = Random.Range(_stutterMinInterval, _stutterMaxInterval);
            }

            _burstTimer -= deltaTime;
            if (_burstTimer > 0.0f) return;

            _isBursting = false;
            _light.intensity = _baseIntensity;
            _calmTimer = Random.Range(_minCalm, _maxCalm);
        }

        #endregion
    }
}
