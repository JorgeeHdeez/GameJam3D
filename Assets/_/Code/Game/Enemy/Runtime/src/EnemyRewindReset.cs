using Core.Runtime;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy.Runtime
{
    /// <summary>
    /// Freezes an enemy during the rewind and snaps it back to its starting pose and
    /// awareness when the run restarts, supporting the game's repetition loop. Captures
    /// the initial pose on enable, listens to the shared game state, and toggles the
    /// enemy's behaviours plus its NavMeshAgent accordingly. Reacts purely to state
    /// changes, so it never references the player or the rewind controller.
    /// </summary>
    public sealed class EnemyRewindReset : MonoBehaviour
    {
        #region Fields

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private NavMeshAgent _agent;
        [SerializeField] private EnemyPerception _perception;
        [Tooltip("Patrol, chase, animator driver, perception, ... - disabled while frozen.")]
        [SerializeField] private MonoBehaviour[] _behaviours;

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;

            if (_gameState == null) return;
            _gameState.OnStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (_gameState == null) return;
            _gameState.OnStateChanged -= OnStateChanged;
        }

        #endregion


        #region Private Fields

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        #endregion


        #region Private Methods

        private void OnStateChanged(GameState state)
        {
            // Enemies only act during play; any other state (rewind, won) freezes them.
            SetFrozen(state != GameState.Playing);

            // The rewind ends by returning to Playing - that is our cue to reset.
            if (state == GameState.Playing) ResetToStart();
        }

        private void SetFrozen(bool isFrozen)
        {
            if (_behaviours != null)
            {
                for (int i = 0; i < _behaviours.Length; i++)
                {
                    if (_behaviours[i] != null) _behaviours[i].enabled = !isFrozen;
                }
            }

            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = isFrozen;
        }

        private void ResetToStart()
        {
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.Warp(_initialPosition);
                _agent.ResetPath();
            }

            transform.SetPositionAndRotation(_initialPosition, _initialRotation);

            if (_perception != null) _perception.ResetPerception();
        }

        #endregion
    }
}
