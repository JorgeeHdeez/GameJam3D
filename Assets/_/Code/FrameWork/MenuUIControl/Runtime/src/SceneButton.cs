using UnityEngine;
using UnityEngine.SceneManagement;

namespace MenuUiControl.Runtime
{
    public enum SceneButtonAction
    {
        LoadScene,
        UnloadSelf,
        TogglePause,
        Quit
    }

    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class SceneButton : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private SceneButtonAction m_action;
        [SerializeField] private string m_sceneName;
        [SerializeField] private bool m_isOverlay;
        [SerializeField] private GameState m_setState = GameState.MainMenu;

        #endregion


        #region Unity Lifecycle

        private void Start()
        {
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(Execute);
        }

        private void OnDestroy()
        {
            var btn = GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.onClick.RemoveListener(Execute);
        }

        #endregion


        #region Main

        public void Execute()
        {
            switch (m_action)
            {
                case SceneButtonAction.LoadScene:
                    LoadTarget();
                    break;

                case SceneButtonAction.UnloadSelf:
                    UnloadOwnerScene();
                    if (GameManager.Instance != null) GameManager.Instance.TogglePause();
                    break;

                case SceneButtonAction.TogglePause:
                    if (GameManager.Instance != null) GameManager.Instance.TogglePause();
                    break;

                case SceneButtonAction.Quit:
                    if (GameManager.Instance != null) GameManager.Instance.QuitGame();
                    break;
            }
        }

        #endregion


        #region Private Methods

        private void LoadTarget()
        {
            if (string.IsNullOrEmpty(m_sceneName)) return;

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(m_setState);

            if (m_isOverlay)
                SceneManager.LoadSceneAsync(m_sceneName, LoadSceneMode.Additive);
            else
                SceneManager.LoadSceneAsync(m_sceneName, LoadSceneMode.Single);
        }

        private void UnloadOwnerScene()
        {
            var scene = gameObject.scene;
            if (scene.isLoaded)
                SceneManager.UnloadSceneAsync(scene);
        }

        #endregion

    }
}
