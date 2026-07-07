using Core.Runtime;
using UnityEngine;

namespace Enemy.Runtime
{
    /// <summary>
    /// Drives an enemy's Animator from its actual motion and awareness: a planar speed
    /// parameter feeds a simple walk/run blend tree, and the whole clip playback is
    /// sped up while chasing (Alerted) for an unsettling, too-fast gait. Speed is
    /// measured from the transform's own displacement, so it works the same whether the
    /// enemy is moved by patrol, chase or (later) a NavMeshAgent. Receives its tick
    /// from the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class EnemyAnimatorDriver : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private EnemyPerception _perception;
        [SerializeField] private Animator _animator;

        [Header("Locomotion Blend")]
        [Tooltip("Animator float parameter that drives the walk/run blend tree.")]
        [SerializeField] private string _speedParameter = "Speed";
        [SerializeField] private float _speedDamping = 8.0f;

        [Header("Chase Playback")]
        [Tooltip("Animator.speed while patrolling / investigating.")]
        [SerializeField] private float _normalPlaybackSpeed = 1.0f;
        [Tooltip("Animator.speed while chasing (Alerted) - >1 gives the creepy sped-up gait.")]
        [SerializeField] private float _chasePlaybackSpeed = 1.6f;
        [SerializeField] private float _playbackLerpSpeed = 4.0f;

        #endregion


        #region Properties

        /// <summary>Last smoothed planar speed pushed to the blend tree.</summary>
        public float CurrentSpeed => _smoothedSpeed;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (_animator == null) return;

            float measured = MeasurePlanarSpeed(deltaTime);
            _smoothedSpeed = Mathf.MoveTowards(_smoothedSpeed, measured, _speedDamping * deltaTime);
            _animator.SetFloat(_speedHash, _smoothedSpeed);

            float targetPlayback = IsChasing() ? _chasePlaybackSpeed : _normalPlaybackSpeed;
            _playback = Mathf.MoveTowards(_playback, targetPlayback, _playbackLerpSpeed * deltaTime);
            _animator.speed = _playback;
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _speedHash = Animator.StringToHash(_speedParameter);
            _lastPosition = transform.position;
            _playback = _normalPlaybackSpeed;

            _updateManager.Register(this);
        }

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private int _speedHash;
        private Vector3 _lastPosition;
        private float _smoothedSpeed;
        private float _playback = 1.0f;

        #endregion


        #region Private Methods

        private bool IsChasing() => _perception != null && _perception.CurrentState == PerceptionState.Alerted;

        // Speed comes from real displacement rather than a driver-supplied value, so it
        // stays correct no matter which system is actually moving the enemy.
        private float MeasurePlanarSpeed(float deltaTime)
        {
            if (deltaTime <= 0.0f) return _smoothedSpeed;

            Vector3 delta = transform.position - _lastPosition;
            delta.y = 0.0f;
            _lastPosition = transform.position;

            return delta.magnitude / deltaTime;
        }

        #endregion
    }
}
