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
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureEventSystem();
        }

        #endregion


        #region Properties

        public static GameManager Instance { get; private set; }
        public static GameState CurrentState { get; private set; } = GameState.MainMenu;
        public static event Action<GameState> OnGameStateChanged;

        #endregion


        #region Main

        public void SetState(GameState newState)
        {
            if (newState == CurrentState) return;
            CurrentState = newState;

            Time.timeScale = (newState == GameState.Paused
                || newState == GameState.GameOver
                || newState == GameState.Won) ? 0f : 1f;

            OnGameStateChanged?.Invoke(CurrentState);
        }

        public void TogglePause()
        {
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

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            DontDestroyOnLoad(go);
        }

        #endregion

    }
}
