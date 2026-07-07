using Core.Runtime;
using UnityEngine;

namespace Enemy.Runtime
{
    /// <summary>
    /// Charges straight at the target while perception is Alerted, taking over from
    /// patrol. Holds as soon as perception drops back to Suspicious or Unaware, so
    /// <see cref="EnemyPatrol"/> naturally resumes investigate/patrol on its own tick
    /// (it already holds itself while Alerted, so the two never fight over movement).
    /// Receives its tick from the <see cref="UpdateManager"/>.
    /// </summary>
    public sealed class EnemyChase : MonoBehaviour, IUpdatable
    {
        #region Fields

        [SerializeField] private UpdateManager _updateManager;
        [SerializeField] private EnemyPerception _perception;
        [SerializeField] private Transform _target;
        [SerializeField] private VoidEventChannelSO _catchChannel;
        [SerializeField] private float _chaseSpeed = 3.5f;
        [SerializeField] private float _turnSpeed = 540.0f;
        [SerializeField] private float _catchDistance = 0.6f;

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

            Chase(deltaTime);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable() => _updateManager.Register(this);

        private void OnDisable() => _updateManager.Unregister(this);

        #endregion


        #region Private Fields

        private bool _hasCaughtTarget;

        #endregion


        #region Private Constants

        private const float DirectionEpsilon = 0.0001f;

        #endregion


        #region Private Methods

        private bool IsAlerted() => _perception != null && _perception.CurrentState == PerceptionState.Alerted;

        private void Chase(float deltaTime)
        {
            if (_target == null) return;

            Vector3 toTarget = _target.position - transform.position;
            toTarget.y = 0.0f;

            if (toTarget.sqrMagnitude <= _catchDistance * _catchDistance)
            {
                RaiseCatchOnce();
                return;
            }

            _hasCaughtTarget = false;

            Vector3 direction = toTarget.normalized;
            transform.position += direction * (_chaseSpeed * deltaTime);
            FaceDirection(direction, deltaTime);
        }

        // Edge-triggered so the catch event fires exactly once per approach, even
        // while the enemy stays within catch distance for several frames.
        private void RaiseCatchOnce()
        {
            if (_hasCaughtTarget) return;

            _hasCaughtTarget = true;
            if (_catchChannel != null) _catchChannel.Raise();
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