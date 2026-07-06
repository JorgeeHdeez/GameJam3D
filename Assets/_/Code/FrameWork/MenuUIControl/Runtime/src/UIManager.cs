#pragma warning disable CS4014

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MenuUiControl.Runtime
{
    public class UIManager : MonoBehaviour
    {

        #region Inspector

        [SerializeField] private UIManagerConfig m_config;
        [SerializeField] private UIScreen m_transitionScreen;

        #endregion


        #region Unity Lifecycle

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
        }

        private void Start()
        {
            if (m_config == null)
            {
                Debug.LogError("[UI MANAGER]: Aucune config assignée dans l'Inspector !");
                return;
            }

            bool anyLoadOnStart = false;
            foreach (var screen in m_config.m_screens)
            {
                if (screen != null && screen.m_loadOnStart)
                {
                    anyLoadOnStart = true;
                    Debug.Log($"[UI MANAGER]: LoadOnStart → '{screen.name}'");
                    Show(screen);
                }
            }

            if (!anyLoadOnStart)
                Debug.Log("[UI MANAGER]: Aucun screen en LoadOnStart");
        }

        #endregion


        #region Properties

        public static UIManager Instance { get; private set; }

        public bool CanPause
        {
            get
            {
                if (m_config == null) return true;
                foreach (var screen in m_config.m_screens)
                {
                    if (screen == null || !screen.m_blockPause) continue;
                    var path = screen.m_scene?.ScenePath;
                    if (!string.IsNullOrEmpty(path) && IsLoaded(path)) return false;
                }
                return true;
            }
        }

        #endregion


        #region Main API — by name

        public void Show(string screenName) => Show(GetScreen(screenName));
        public Task ShowAsync(string screenName) => ShowAsync(GetScreen(screenName));
        public void Hide(string screenName) => Hide(GetScreen(screenName));
        public Task HideAsync(string screenName) => HideAsync(GetScreen(screenName));
        public void Toggle(string screenName) => Toggle(GetScreen(screenName));

        #endregion


        #region Main API — by asset

        public void Show(UIScreen screen)
        {
            if (!HasScene(screen)) return;
            _ = LoadScreenAsync(screen);
        }

        public async Task ShowAsync(UIScreen screen)
        {
            if (!HasScene(screen)) return;
            await LoadScreenAsync(screen);
        }

        public void Hide(UIScreen screen)
        {
            if (screen == null) return;
            _ = UnloadScreenAsync(screen);
        }

        public async Task HideAsync(UIScreen screen)
        {
            if (screen == null) return;
            await UnloadScreenAsync(screen);
        }

        public void Toggle(UIScreen screen)
        {
            if (!HasScene(screen)) return;

            if (IsLoaded(screen.m_scene.ScenePath))
                _ = UnloadScreenAsync(screen);
            else
                _ = LoadScreenAsync(screen);
        }

        public async Task Reload(UIScreen screen)
        {
            if (screen == null) return;
            await HideAsync(screen);
            await ShowAsync(screen);
        }

        #endregion


        #region Main API — global

        public void HideAll()
        {
            if (m_config == null) return;
            foreach (var screen in m_config.m_screens)
                Hide(screen);
        }

        #endregion


        #region Private Methods

        private UIScreen GetScreen(string screenName)
        {
            if (m_config == null) return null;
            var screen = m_config.m_screens.Find(s => s != null && s.name == screenName);
            if (screen == null)
                Debug.LogWarning($"[UI MANAGER]: Screen '{screenName}' introuvable dans la config");
            return screen;
        }

        private bool HasScene(UIScreen screen)
        {
            if (screen == null)
            {
                Debug.LogWarning("[UI MANAGER]: Screen null");
                return false;
            }
            if (screen.m_scene == null || string.IsNullOrEmpty(screen.m_scene.ScenePath))
            {
                Debug.LogError($"[UI MANAGER]: '{screen.name}' → ScenePath VIDE. " +
                    "Ouvre le tool UI Manager et clique 'Sync All'.");
                return false;
            }
            return true;
        }

        private bool IsLoaded(string path)
        {
            var scene = SceneManager.GetSceneByPath(path);
            return scene.isLoaded;
        }

        private async Task LoadScreenAsync(UIScreen screen)
        {
            var path = screen.m_scene.ScenePath;
            var mgrPath = m_config?.m_managerScene?.ScenePath;

            // Transition — uniquement pour les swaps non-overlay
            if (m_transitionScreen != null && !screen.m_isOverlay && screen != m_transitionScreen)
                await ShowAsync(m_transitionScreen);

            if (!string.IsNullOrEmpty(mgrPath) && !IsLoaded(mgrPath))
            {
                var managerOp = SceneManager.LoadSceneAsync(mgrPath, LoadSceneMode.Additive);
                if (managerOp != null)
                {
                    while (!managerOp.isDone)
                        await Task.Yield();
                }
            }

            if (!screen.m_isOverlay)
                UnloadNonOverlayScenes(screen, mgrPath);

            if (!IsLoaded(path))
            {
                var op = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                if (op == null)
                {
                    Debug.LogError($"[UI MANAGER]: Impossible de charger '{path}' — " +
                        "la scène n'est PAS dans les Build Settings. " +
                        "Ouvre le tool UI Manager et clique 'Sync All'.");
                    return;
                }

                while (!op.isDone)
                    await Task.Yield();

                var scene = SceneManager.GetSceneByPath(path);
                if (!screen.m_isOverlay && scene.isLoaded)
                    SceneManager.SetActiveScene(scene);

                Debug.Log($"[UI MANAGER]: '{screen.name}' chargé (overlay={screen.m_isOverlay})");
            }

            // Cache la transition après le chargement
            if (m_transitionScreen != null && !screen.m_isOverlay && screen != m_transitionScreen)
                Hide(m_transitionScreen);
        }

        private void UnloadNonOverlayScenes(UIScreen screen, string mgrPath)
        {
            foreach (var s in m_config.m_screens)
            {
                if (s == null || s == screen || s == m_transitionScreen || s.m_isOverlay) continue;
                var sp = s.m_scene?.ScenePath;
                if (!string.IsNullOrEmpty(sp) && IsLoaded(sp)) _ = UnloadScreenAsync(s);
            }

            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!string.IsNullOrEmpty(mgrPath) && scene.path == mgrPath) continue;
                if (m_transitionScreen != null && m_transitionScreen.m_scene?.ScenePath == scene.path) continue;

                bool isOverlayUI = m_config.m_screens.Exists(s =>
                    s != null && s.m_isOverlay && s.m_scene?.ScenePath == scene.path);
                if (isOverlayUI) continue;

                if (scene.isLoaded)
                    SceneManager.UnloadSceneAsync(scene);
            }
        }

        private async Task UnloadScreenAsync(UIScreen screen)
        {
            var path = screen.m_scene?.ScenePath;
            if (string.IsNullOrEmpty(path)) return;
            if (!IsLoaded(path)) return;

            var op = SceneManager.UnloadSceneAsync(path);
            if (op == null) return;
            while (!op.isDone)
                await Task.Yield();
        }

        private void FireTrigger(UITrigger trigger)
        {
            if (m_config == null) return;
            foreach (var screen in m_config.m_screens)
            {
                if (screen == null) continue;
                if (screen.m_showTrigger == trigger) Show(screen);
                if (screen.m_hideTrigger == UIHideTrigger.All) Hide(screen);
                else if ((int)screen.m_hideTrigger == (int)trigger) Hide(screen);
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Paused) FireTrigger(UITrigger.OnPause);
            else if (state == GameState.Playing) FireTrigger(UITrigger.OnResume);
            else if (state == GameState.GameOver) FireTrigger(UITrigger.OnGameOver);
            else if (state == GameState.Won) FireTrigger(UITrigger.OnVictory);
        }

        #endregion

    }
}
