using Core.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Runtime
{
    /// <summary>
    /// Moves the enemy along a looping list of waypoints via a <see cref="NavMeshAgent"/>,
    /// so it paths around walls and obstacles instead of walking through them. A heard
    /// noise ping makes the enemy leave the patrol to investigate the source, look around
    /// for a moment, then resume. Patrol holds while the enemy is alerted (sees the
    /// player), leaving the reaction to perception and the chase. The agent owns position
    /// and facing, so the vision cone naturally sweeps the travel direction. Receives its
    /// tick from the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class EnemyPatrol : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private EnemyPerception _perception;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private NoisePingEventChannelSO _noisePingChannel;
        [SerializeField] private Transform[] _waypoints;
        [SerializeField] private float _patrolSpeed = 2.0f;
        [SerializeField] private float _arriveDistance = 0.4f;
        [SerializeField] private float _waitTime = 1.0f;
        [SerializeField] private float _investigateLookTime = 2.0f;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (IsAlerted()) return;

            // A sustained sound sends the patrol to investigate where the player was
            // heard; repeated cues refresh the target so it trails the noise.
            if (_perception != null && _perception.TryConsumeHeardPosition(out Vector3 heard))
            {
                _investigateTarget = heard;
                _hasInvestigateTarget = true;
                _investigateTimer = 0.0f;
            }

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


        #region Private Methods

        // Hold the patrol only when the enemy actually sees the player; reaction is then
        // left to perception and the chase.
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
            if (_agent == null) return;

            _agent.speed = _patrolSpeed;
            _agent.SetDestination(_investigateTarget);

            if (!HasArrived()) return;

            _investigateTimer += deltaTime;
            if (_investigateTimer >= _investigateLookTime) _hasInvestigateTarget = false;
        }

        private void Patrol(float deltaTime)
        {
            if (!HasWaypoints() || _agent == null) return;

            _agent.speed = _patrolSpeed;

            if (_waitTimer > 0.0f)
            {
                _waitTimer -= deltaTime;
                return;
            }

            Transform target = _waypoints[_currentIndex];
            if (target == null)
            {
                AdvanceWaypoint();
                return;
            }

            _agent.SetDestination(target.position);

            if (HasArrived()) AdvanceWaypoint();
        }

        private void AdvanceWaypoint()
        {
            _waitTimer = _waitTime;
            _currentIndex = (_currentIndex + 1) % _waypoints.Length;
        }

        // Arrived once the agent has a resolved path and is within reach of the target.
        private bool HasArrived()
        {
            if (_agent.pathPending) return false;

            return _agent.remainingDistance <= _arriveDistance;
        }

        #endregion
    }
}