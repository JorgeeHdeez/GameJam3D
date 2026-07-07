using Core.Runtime;
using UnityEngine;

namespace Enemy.Runtime
{
    /// <summary>
    /// Perceives the player through hearing and a vision cone, feeding a progressive
    /// detection meter that drives an Unaware / Suspicious / Alerted state. Sight
    /// fills the meter fully; noise only raises it to a cap. Walls block sight via a
    /// line-of-sight raycast, so the enemy cannot see through them. Receives its tick
    /// from the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class EnemyPerception : MonoBehaviour, IUpdatable, IDetectionSource
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private Transform _target;
        [SerializeField] private FloatEventChannelSO _noiseChannel;
        [SerializeField] private PerceptionStateEventChannelSO _stateChannel;
        [SerializeField] private VoidEventChannelSO _alarmChannel;
        [SerializeField] private NoisePingEventChannelSO _noisePingChannel;
        [SerializeField] private DetectionRegistrySO _detectionRegistry;
        [SerializeField] private PlayerVisibilitySO _playerVisibility;

        [Header("Vision Cone")]
        [SerializeField] private float _eyeHeight = 1.6f;
        [SerializeField] private float _targetHeight = 1.2f;
        [SerializeField] private float _viewDistance = 12.0f;
        [SerializeField] private float _viewHalfAngle = 55.0f;
        [SerializeField] private LayerMask _obstacleLayer;

        [Header("Detection Meter")]
        [SerializeField] private float _sightGainPerSecond = 1.5f;
        [SerializeField] private float _hearingGainPerSecond = 0.6f;
        [SerializeField] private float _decayPerSecond = 0.4f;
        [SerializeField] private float _hearingCap = 0.7f;
        [SerializeField] private float _suspiciousThreshold = 0.4f;
        [SerializeField] private float _alertedThreshold = 0.95f;
        [SerializeField] private float _pingDetectionBump = 0.5f;
        [Tooltip("Continuous hearing time (s) before the enemy commits to investigating the sound.")]
        [SerializeField] private float _hearingReactionTime = 0.5f;

        #endregion


        #region Properties

        /// <summary>Current awareness state.</summary>
        public PerceptionState CurrentState => _currentState;

        /// <summary>Detection meter, 0 (unaware) to 1 (fully alerted).</summary>
        public float DetectionLevel => _detectionLevel;

        #endregion


        #region Public API

        /// <summary>Clears the detection meter and awareness, e.g. on a rewind reset.</summary>
        public void ResetPerception()
        {
            _detectionLevel = 0.0f;
            _currentNoiseRadius = 0.0f;
            _currentState = PerceptionState.Unaware;
            _hearingTimer = 0.0f;
            _hasPendingHeard = false;

            if (_stateChannel != null) _stateChannel.Raise(_currentState);
        }

        /// <summary>
        /// Hands out a fresh "go look here" cue raised by sustained hearing, consuming
        /// it so it fires once per commit. Returns false when there is nothing pending.
        /// </summary>
        public bool TryConsumeHeardPosition(out Vector3 position)
        {
            position = _pendingHeardPosition;
            if (!_hasPendingHeard) return false;

            _hasPendingHeard = false;
            return true;
        }

        public void Tick(float deltaTime)
        {
            bool sees = CanSeeTarget();
            bool hears = HearsTarget();

            if (sees)
            {
                _detectionLevel = Mathf.MoveTowards(_detectionLevel, 1.0f, _sightGainPerSecond * deltaTime);
            }
            else if (hears && _detectionLevel < _hearingCap)
            {
                _detectionLevel = Mathf.MoveTowards(_detectionLevel, _hearingCap, _hearingGainPerSecond * deltaTime);
            }
            else
            {
                _detectionLevel = Mathf.MoveTowards(_detectionLevel, 0.0f, _decayPerSecond * deltaTime);
            }

            UpdateHearingCue(hears, deltaTime);
            UpdateState();
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _updateManager.Register(this);

            if (_detectionRegistry != null) _detectionRegistry.Register(this);

            if (_noiseChannel != null) _noiseChannel.OnEventRaised += OnNoiseRaised;
            if (_noisePingChannel != null) _noisePingChannel.OnEventRaised += OnNoisePing;
        }

        private void OnDisable()
        {
            _updateManager.Unregister(this);

            if (_detectionRegistry != null) _detectionRegistry.Unregister(this);

            if (_noiseChannel != null) _noiseChannel.OnEventRaised -= OnNoiseRaised;
            if (_noisePingChannel != null) _noisePingChannel.OnEventRaised -= OnNoisePing;
        }

        #endregion


        #region Private Fields

        private float _detectionLevel;
        private float _currentNoiseRadius;
        private PerceptionState _currentState = PerceptionState.Unaware;
        private float _hearingTimer;
        private Vector3 _pendingHeardPosition;
        private bool _hasPendingHeard;

        #endregion


        #region Private Methods

        private void OnNoiseRaised(float radius) => _currentNoiseRadius = radius;

        // Sustained hearing arms a one-shot "investigate" cue at the target's current
        // position, then re-arms: while the player keeps making noise the cue fires
        // periodically, so a listening patrol keeps refreshing its target and follows
        // the sound. Going quiet stops the cues and lets the patrol give up and resume.
        private void UpdateHearingCue(bool hears, float deltaTime)
        {
            if (!hears)
            {
                _hearingTimer = 0.0f;
                return;
            }

            _hearingTimer += deltaTime;
            if (_hearingTimer < _hearingReactionTime) return;

            _hearingTimer = 0.0f;
            _pendingHeardPosition = _target.position;
            _hasPendingHeard = true;
        }

        // A heard knock spikes the detection meter toward suspicion, so the gauge
        // reacts even though the sound is momentary.
        private void OnNoisePing(NoisePing ping)
        {
            if ((transform.position - ping.Position).sqrMagnitude > ping.Radius * ping.Radius) return;

            _detectionLevel = Mathf.Max(_detectionLevel, _pingDetectionBump);
        }

        // The enemy sees the target if it is within range, inside the cone half-angle,
        // and not occluded by an obstacle along the line of sight.
        private bool CanSeeTarget()
        {
            if (_target == null) return false;
            if (_playerVisibility != null && _playerVisibility.IsHidden) return false;

            Vector3 eye = transform.position + Vector3.up * _eyeHeight;
            Vector3 toTarget = (_target.position + Vector3.up * _targetHeight) - eye;
            float distance = toTarget.magnitude;

            if (distance > _viewDistance) return false;
            if (Vector3.Angle(transform.forward, toTarget) > _viewHalfAngle) return false;
            if (Physics.Raycast(eye, toTarget.normalized, distance, _obstacleLayer, QueryTriggerInteraction.Ignore)) return false;

            return true;
        }

        // The enemy hears the target if it sits within the player's current noise radius.
        private bool HearsTarget()
        {
            if (_target == null) return false;
            if (_playerVisibility != null && _playerVisibility.IsHidden) return false;
            if (_currentNoiseRadius <= 0.0f) return false;

            float sqrDistance = (_target.position - transform.position).sqrMagnitude;
            return sqrDistance <= _currentNoiseRadius * _currentNoiseRadius;
        }

        private void UpdateState()
        {
            PerceptionState next = ResolveState();
            if (next == _currentState) return;

            _currentState = next;

            if (next == PerceptionState.Alerted && _alarmChannel != null) _alarmChannel.Raise();

            if (_stateChannel == null) return;
            _stateChannel.Raise(next);
        }

        private PerceptionState ResolveState()
        {
            if (_detectionLevel >= _alertedThreshold) return PerceptionState.Alerted;
            if (_detectionLevel >= _suspiciousThreshold) return PerceptionState.Suspicious;

            return PerceptionState.Unaware;
        }

        #endregion
    }
}