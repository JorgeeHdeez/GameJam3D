using Core.Runtime;
using UnityEngine;

namespace Player.Runtime
{
    /// <summary>
    /// The level exit: when the player reaches it during play, the run is won. Raises
    /// a win event so UI / audio can react, and flips the game state to Won (which
    /// stops recording and lets other systems settle). Needs a trigger collider.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ExitCheckpoint : MonoBehaviour
    {
        #region Fields

        [SerializeField] private GameStateSO _gameState;
        [SerializeField] private VoidEventChannelSO _wonChannel;

        #endregion


        #region Unity Callbacks

        private void OnTriggerEnter(Collider other)
        {
            if (_gameState == null || _gameState.CurrentState != GameState.Playing) return;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            _gameState.SetState(GameState.Won);

            if (_wonChannel != null) _wonChannel.Raise();
        }

        #endregion
    }
}
