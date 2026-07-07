using Core.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Hud.Runtime
{
    /// <summary>
    /// Drives a single on-screen detection meter from the shared detection registry, so
    /// it reflects how close the most aware enemy in the level is to spotting the player
    /// rather than any one enemy. The fill eases toward that global maximum and the
    /// colour steps through unaware / suspicious / alerted by threshold. Reads the
    /// registry each tick, so the readout is fully deterministic. Ticked by the
    /// <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class DetectionGaugeUI : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private DetectionRegistrySO _detectionRegistry;
        [SerializeField] private Image _fillImage;
        [SerializeField] private float _fillLerpSpeed = 6.0f;

        [Header("State Thresholds")]
        [Tooltip("Keep in sync with the enemies' perception thresholds for a matching readout.")]
        [SerializeField] private float _suspiciousThreshold = 0.4f;
        [SerializeField] private float _alertedThreshold = 0.95f;

        [Header("State Colors")]
        [SerializeField] private Color _unawareColor = new(1.0f, 1.0f, 1.0f, 0.4f);
        [SerializeField] private Color _suspiciousColor = new(1.0f, 0.6f, 0.0f, 1.0f);
        [SerializeField] private Color _alertedColor = new(1.0f, 0.15f, 0.1f, 1.0f);

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (_detectionRegistry == null || _fillImage == null) return;

            float target = _detectionRegistry.MaxDetectionLevel;
            _displayedLevel = Mathf.MoveTowards(_displayedLevel, target, _fillLerpSpeed * deltaTime);

            _fillImage.fillAmount = _displayedLevel;
            _fillImage.color = ResolveColor(_displayedLevel);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private float _displayedLevel;

        #endregion


        #region Private Methods

        private Color ResolveColor(float level)
        {
            if (level >= _alertedThreshold) return _alertedColor;
            if (level >= _suspiciousThreshold) return _suspiciousColor;

            return _unawareColor;
        }

        #endregion
    }
}
