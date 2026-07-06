using Core.Runtime;
using UnityEngine;

namespace Enemy.Runtime
{
    /// <summary>
    /// Moves the enemy along a looping list of waypoints, facing its travel direction
    /// so the vision cone sweeps the path. A heard noise ping makes the enemy leave the
    /// patrol to investigate the source, look around for a moment, then resume. Patrol
    /// holds while the enemy is alerted (sees the player), leaving reaction to perception
    /// and the alarm. Receives its tick from the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class EnemyPatrol : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private EnemyPerception _perception;
        [SerializeField] private NoisePingEventChannelSO _noisePingChannel;
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _moveSpeed = 2.0f;
        [SerializeField] private float _turnSpeed = 360.0f;
        [SerializeField] private float _arriveDistance = 0.3f;
        [SerializeField] private float _waitTime = 1.0f;
        [SerializeField] private float _investigateLookTime = 2.0f;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (IsAlerted()) return;

            if (_hasInvestigateTarget)
            {
                Investigate(deltaTime);
                return;
            }

            Patrol(deltaTime);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _updateManager.Register(this);

            if (_noisePingChannel != null) _noisePingChannel.OnEventRaised += OnNoisePing;
        }

        private void OnDisable()
        {
            _updateManager.Unregister(this);

            if (_noisePingChannel != null) _noisePingChannel.OnEventRaised -= OnNoisePing;
        }

        private void OnDrawGizmosSelected()
        {
            if (!HasWaypoints()) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < _waypoints.Length; i++)
            {
                if (_waypoints[i] == null) continue;

                Gizmos.DrawWireSphere(_waypoints[i].position, 0.3f);

                Transform next = _waypoints[(i + 1) % _waypoints.Length];
                if (next != null) Gizmos.DrawLine(_waypoints[i].position, next.position);
            }
        }

        #endregion


        #region Private Fields

        private int _currentIndex;
        private float _waitTimer;
        private bool _hasInvestigateTarget;
        private Vector3 _investigateTarget;
        private float _investigateTimer;

        #endregion


        #region Private Constants

        private const float DirectionEpsilon = 0.0001f;

        #endregion


        #region Private Methods

        // Hold the patrol only when the enemy actually sees the player; reaction is then
        // left to perception and the alarm.
        private bool IsAlerted() => _perception != null && _perception.CurrentState == PerceptionState.Alerted;

        private bool HasWaypoints() => _waypoints != null && _waypoints.Length > 0;

        private void OnNoisePing(NoisePing ping)
        {
            if ((transform.position - ping.Position).sqrMagnitude > ping.Radius * ping.Radius) return;

            _investigateTarget = ping.Position;
            _hasInvestigateTarget = true;
            _investigateTimer = 0.0f;
        }

        private void Investigate(float deltaTime)
        {
            Vector3 toTarget = _investigateTarget - transform.position;
            toTarget.y = 0.0f;

            if (toTarget.sqrMagnitude <= _arriveDistance * _arriveDistance)
            {
                _investigateTimer += deltaTime;
                if (_investigateTimer >= _investigateLookTime) _hasInvestigateTarget = false;
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * (_moveSpeed * deltaTime);
            FaceDirection(direction, deltaTime);
        }

        private void Patrol(float deltaTime)
        {
            if (!HasWaypoints()) return;

            if (_waitTimer > 0.0f)
            {
                _waitTimer -= deltaTime;
                return;
            }

            Vector3 toTarget = _waypoints[_currentIndex].position - transform.position;
            toTarget.y = 0.0f;

            if (toTarget.sqrMagnitude <= _arriveDistance * _arriveDistance)
            {
                AdvanceWaypoint();
                return;
            }

            Vector3 direction = toTarget.normalized;
            transform.position += direction * (_moveSpeed * deltaTime);
            FaceDirection(direction, deltaTime);
        }

        private void AdvanceWaypoint()
        {
            _waitTimer = _waitTime;
            _currentIndex = (_currentIndex + 1) % _waypoints.Length;
        }

        private void FaceDirection(Vector3 direction, float deltaTime)
        {
            if (direction.sqrMagnitude < DirectionEpsilon) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * deltaTime);
        }

        #endregion
    }
}