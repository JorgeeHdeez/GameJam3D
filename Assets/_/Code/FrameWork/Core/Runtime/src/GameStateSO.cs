using System;
using UnityEngine;

namespace Core.Runtime
{
    /// <summary>
    /// Single source of truth for the run's flow state, injected as an asset. Systems
    /// react to changes through <see cref="OnStateChanged"/> without referencing each
    /// other (the player suspends control during a rewind, enemies freeze and reset,
    /// post-processing ramps its rewind look, ...). Reset to Playing on load so a stale
    /// value never carries over between editor play sessions.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Game State", fileName = "GameState")]
    public sealed class GameStateSO : ScriptableObject
    {
        #region Fields

        /// <summary>Raised whenever the state actually changes.</summary>
        public event Action<GameState> OnStateChanged;

        #endregion


        #region Properties

        public GameState CurrentState => _currentState;

        #endregion


        #region Public API

        public void SetState(GameState state)
        {
            if (state == _currentState) return;

            _currentState = state;
            OnStateChanged?.Invoke(state);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable() => _currentState = GameState.Playing;

        #endregion


        #region Private Fields

        private GameState _currentState = GameState.Playing;

        #endregion
    }
}
