using UnityEngine;

namespace MenuUiControl.Runtime
{
    public enum ButtonActionType
    {
        ShowScreen,
        HideScreen,
        HideCurrent,
        HideAll,
        Resume,
        StartGame,
        RestartGame,
        MainMenu,
        Quit
    }

    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class UIButtonAction : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private ButtonActionType m_actionType;
        [SerializeField] private UIScreen m_targetScreen;
        [SerializeField] private UIScreen m_currentScreen;

        #endregion


        #region Unity Lifecycle

        private void Start()
        {
            var btn = GetComponent<UnityEngine.UI.Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(Execute);
        }

        private void OnDestroy()
        {
            var btn = GetComponent<UnityEngine.UI.Button>();
            if (btn != null) btn.onClick.RemoveListener(Execute);
        }

        #endregion


        #region Properties

        public ButtonActionType ActionType
        {
            get => m_actionType;
            set => m_actionType = value;
        }

        public UIScreen TargetScreen
        {
            get => m_targetScreen;
            set => m_targetScreen = value;
        }

        public UIScreen CurrentScreen
        {
            get => m_currentScreen;
            set => m_currentScreen = value;
        }

        #endregion


        #region Main API

        public void Execute()
        {
            var ui = UIManager.Instance;
            if (ui == null) return;

            switch (m_actionType)
            {
                case ButtonActionType.ShowScreen:
                    if (m_targetScreen != null) ui.Show(m_targetScreen);
                    break;

                case ButtonActionType.HideScreen:
                    if (m_targetScreen != null) ui.Hide(m_targetScreen);
                    break;

                case ButtonActionType.HideCurrent:
                    if (m_currentScreen != null) ui.Hide(m_currentScreen);
                    break;

                case ButtonActionType.HideAll:
                    ui.HideAll();
                    break;

                case ButtonActionType.Resume:
                    if (GameManager.Instance != null) GameManager.Instance.Resume();
                    break;

                case ButtonActionType.StartGame:
                    if (m_targetScreen != null) ui.Show(m_targetScreen);
                    if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
                    break;

                case ButtonActionType.RestartGame:
                    if (m_targetScreen != null) _ = ui.Reload(m_targetScreen);
                    if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Playing);
                    break;

                case ButtonActionType.MainMenu:
                    if (m_targetScreen != null) ui.Show(m_targetScreen);
                    if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.MainMenu);
                    break;

                case ButtonActionType.Quit:
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        #endregion

    }
}
