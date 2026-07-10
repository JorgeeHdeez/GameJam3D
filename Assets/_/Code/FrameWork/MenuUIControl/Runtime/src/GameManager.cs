using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace MenuUiControl.Runtime
{
    public class GameManager : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private InputActionReference m_pauseAction;
        [SerializeField] private string m_pauseSceneName;

        #endregion


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
        }

        private void OnEnable()
        {
            if (m_pauseAction == null)
            {
                Debug.LogError("[GAME MANAGER]: m_pauseAction non assigné !");
                return;
            }

            m_pauseAction.action.performed += OnPauseInput;
            m_pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            if (m_pauseAction == null) return;
            m_pauseAction.action.performed -= OnPauseInput;
            m_pauseAction.action.Disable();
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

        private void OnPauseInput(InputAction.CallbackContext ctx)
        {
            if (CurrentState != GameState.Playing) return;
            SetState(GameState.Paused);
            if (!string.IsNullOrEmpty(m_pauseSceneName))
                SceneManager.LoadSceneAsync(m_pauseSceneName, LoadSceneMode.Additive);
        }

        #endregion


        #region Private

        #endregion

    }
}
