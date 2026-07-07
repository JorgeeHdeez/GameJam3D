using Core.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Runtime
{
    /// <summary>
    /// A stationary guard that replaces <see cref="EnemyPatrol"/> on static enemies. It
    /// holds a fixed post and facing until it perceives the player: it investigates
    /// sounds it hears, holds while alerted (the chase drives movement then), and once
    /// it has lost the player and has nothing left to investigate it walks back to its
    /// post and turns to face its original guard direction. The post is captured once at
    /// start, so it survives freeze/reset cycles. Receives its tick from the
    /// <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class EnemySentinel : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private EnemyPerception _perception;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private float _returnSpeed = 2.0f;
        [SerializeField] private float _arriveDistance = 0.4f;
        [SerializeField] private float _reorientSpeed = 360.0f;
        [SerializeField] private float _investigateLookTime = 2.0f;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (IsAlerted()) return;

            // A sustained sound sends the guard to investigate; repeated cues trail it.
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

            ReturnToPost(deltaTime);
        }

        #endregion


        #region Unity Callbacks

        // Captured once (not on every enable) so freeze/reset cycles keep the true post.
        private void Awake()
        {
            _postPosition = transform.position;
            _postRotation = transform.rotation;
        }

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private Vector3 _postPosition;
        private Quaternion _postRotation;
        private bool _hasInvestigateTarget;
        private Vector3 _investigateTarget;
        private float _investigateTimer;

        #endregion


        #region Private Methods

        private bool IsAlerted() => _perception != null && _perception.CurrentState == PerceptionState.Alerted;

        private void Investigate(float deltaTime)
        {
            if (_agent == null) return;

            _agent.speed = _returnSpeed;
            _agent.SetDestination(_investigateTarget);

            if (!HasArrived()) return;

            _investigateTimer += deltaTime;
            if (_investigateTimer >= _investigateLookTime) _hasInvestigateTarget = false;
        }

        private void ReturnToPost(float deltaTime)
        {
            if (_agent == null) return;

            if (!IsAtPost())
            {
                _agent.speed = _returnSpeed;
                _agent.SetDestination(_postPosition);
                return;
            }

            // Back on post: hold position and turn back to the original guard facing.
            _agent.SetDestination(_postPosition);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, _postRotation, _reorientSpeed * deltaTime);
        }

        private bool IsAtPost()
        {
            Vector3 toPost = _postPosition - transform.position;
            toPost.y = 0.0f;

            return toPost.sqrMagnitude <= _arriveDistance * _arriveDistance;
        }

        private bool HasArrived()
        {
            if (_agent.pathPending) return false;

            return _agent.remainingDistance <= _arriveDistance;
        }

        #endregion
    }
}
