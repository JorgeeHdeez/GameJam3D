using Core.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Runtime
{
    /// <summary>
    /// Chases the target via a <see cref="NavMeshAgent"/> while perception is Alerted,
    /// pathing around walls to reach the player rather than pushing straight into them.
    /// Takes over from patrol (which holds itself while Alerted) and hands control back
    /// as soon as perception drops below Alerted. Raises a one-shot catch event once it
    /// closes to catch distance. Receives its tick from the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class EnemyChase : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private EnemyPerception _perception;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private Transform _target;
        [SerializeField] private VoidEventChannelSO _catchChannel;
        [SerializeField] private float _chaseSpeed = 3.5f;
        [SerializeField] private float _catchDistance = 0.8f;

        #endregion


        #region Properties

        /// <summary>True once the enemy has closed to catch distance while alerted.</summary>
        public bool HasCaughtTarget => _hasCaughtTarget;

        #endregion


        #region Public API

        public void Tick(float deltaTime)
        {
            if (!IsAlerted())
            {
                _hasCaughtTarget = false;
                return;
            }

            Chase();
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private bool _hasCaughtTarget;

        #endregion


        #region Private Methods

        private bool IsAlerted() => _perception != null && _perception.CurrentState == PerceptionState.Alerted;

        private void Chase()
        {
            if (_target == null || _agent == null) return;

            _agent.speed = _chaseSpeed;
            _agent.SetDestination(_target.position);

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0.0f;

            if (toTarget.sqrMagnitude <= _catchDistance * _catchDistance)
            {
                RaiseCatchOnce();
                return;
            }

            _hasCaughtTarget = false;
        }

        // Edge-triggered so the catch event fires exactly once per approach, even while
        // the enemy stays within catch distance for several frames.
        private void RaiseCatchOnce()
        {
            if (_hasCaughtTarget) return;

            _hasCaughtTarget = true;
            if (_catchChannel != null) _catchChannel.Raise();
        }

        #endregion
    }
}