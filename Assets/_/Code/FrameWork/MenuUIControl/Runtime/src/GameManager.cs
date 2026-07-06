using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MenuUiControl.Runtime
{
    public class GameManager : MonoBehaviour
    {

        #region Unity Lifecycle

        private void Awake()
        {
            Instance = this;
            EnsureEventSystem();
        }

        #endregion


        #region Properties

        public static GameManager Instance { get; private set; }
        public static GameState CurrentState { get; private set; } = GameState.MainMenu;
        public static event Action<GameState> OnGameStateChanged;

        #endregion


        #region Main API

        public void SetState(GameState newState)
        {
            if (newState == CurrentState) return;
            CurrentState = newState;
            Time.timeScale = (newState == GameState.Paused
                || newState == GameState.GameOver
                || newState == GameState.Won) ? 0f : 1f;
            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void Pause() => SetState(GameState.Paused);
        public void Resume() => SetState(GameState.Playing);
        public void GameOver() => SetState(GameState.GameOver);
        public void Win() => SetState(GameState.Won);

        public void OnPlayPause()
        {
            var uiManager = UIManager.Instance;
            if (uiManager != null && !uiManager.CanPause) return;
            if (CurrentState != GameState.Playing && CurrentState != GameState.Paused) return;
            SetState(CurrentState == GameState.Paused ? GameState.Playing : GameState.Paused);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion


        #region Private Methods

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[GAME MANAGER]: EventSystem créé automatiquement");
        }

        #endregion

    }
}
