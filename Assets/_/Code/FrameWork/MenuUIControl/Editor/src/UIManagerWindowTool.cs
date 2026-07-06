using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MenuUiControl.Runtime;

namespace MenuUiControl.Editor
{
    public class UIManagerWindowTool : EditorWindow
    {

        #region Constants

        private const string _screensDatabase = "Assets/_/Database/UIScreens";
        private const string _configPath = "Assets/_/Database/Settings/Resources/UIManagerConfig.asset";
        private const string _configFolder = "Assets/_/Database/Settings/Resources";

        #endregion


        #region Menu

        [MenuItem("Managers/UI Manager")]
        public static void Open()
        {
            var window = GetWindow<UIManagerWindowTool>("UI Manager");
            window.minSize = new Vector2(400, 600);
        }

        #endregion


        #region GUI

        private void OnEnable()
        {
            LoadConfig();
            RefreshScreens();
        }

        private void OnFocus()
        {
            LoadConfig();
            RefreshScreens();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawConfig();
            DrawSeparator();
            DrawScreens();
            DrawSeparator();
            DrawLoadBar();

            EditorGUILayout.EndScrollView();
        }

        #endregion


        #region Draw

        private void DrawConfig()
        {
            EditorGUILayout.LabelField("UI Manager Config", EditorStyles.boldLabel);

            if (_config == null)
            {
                EditorGUILayout.HelpBox("Aucune UIManagerConfig trouvée.", MessageType.Warning);
                if (GUILayout.Button("Créer UIManagerConfig", GUILayout.Height(30)))
                    CreateConfig();
                return;
            }

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Config", _config, typeof(UIManagerConfig), false);
            EditorGUI.EndDisabledGroup();

            var currentMgr = _config.m_managerScene?.m_sceneAsset;
            var newMgr = (SceneAsset)EditorGUILayout.ObjectField(
                "Manager Scene", currentMgr, typeof(SceneAsset), false);
            if (newMgr != currentMgr)
                AssignScene(ref _config.m_managerScene, newMgr, _config);

            EditorGUILayout.Space(5);

            if (GUILayout.Button("⟳ Sync All (paths + Build Settings)", GUILayout.Height(26)))
                SyncAll();
        }

        private void DrawScreens()
        {
            if (_config == null) return;

            EditorGUILayout.LabelField("UI Screens", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            _newScreenName = EditorGUILayout.TextField(_newScreenName);
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_newScreenName));
            if (GUILayout.Button("+ Créer Screen", GUILayout.Width(120)))
            {
                CreateScreen(_newScreenName);
                _newScreenName = "";
                RefreshScreens();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (_screens.Count == 0)
            {
                EditorGUILayout.HelpBox("Aucun UIScreen créé.", MessageType.Info);
                return;
            }

            for (int i = 0; i < _screens.Count; i++)
            {
                var screen = _screens[i];
                if (screen == null) continue;

                bool isSelected = _selectedScreen == screen;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(
                    isSelected ? "▼  " + screen.name : "▶  " + screen.name,
                    EditorStyles.boldLabel))
                    _selectedScreen = isSelected ? null : screen;

                bool hasScene = screen.m_scene != null && screen.m_scene.m_sceneAsset != null;

                EditorGUI.BeginDisabledGroup(Application.isPlaying || !hasScene);
                if (GUILayout.Button("Set", GUILayout.Width(40)))
                    LoadInEditor(screen);
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!Application.isPlaying);
                if (GUILayout.Button("Load", GUILayout.Width(50)))
                {
                    var manager = FindFirstObjectByType<UIManager>();
                    if (manager != null) manager.Show(screen);
                }
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    DeleteScreen(screen);
                    GUIUtility.ExitGUI();
                    return;
                }
                EditorGUILayout.EndHorizontal();

                if (isSelected)
                    DrawScreenDetail(screen);
            }
        }

        private void DrawScreenDetail(UIScreen screen)
        {
            EditorGUI.indentLevel++;

            // --- Scène assignée ---
            var currentAsset = screen.m_scene?.m_sceneAsset;
            var newAsset = (SceneAsset)EditorGUILayout.ObjectField(
                "Scène", currentAsset, typeof(SceneAsset), false);
            if (newAsset != currentAsset)
                AssignScene(ref screen.m_scene, newAsset, screen);

            var syncedPath = screen.m_scene?.ScenePath;
            if (currentAsset != null && string.IsNullOrEmpty(syncedPath))
            {
                EditorGUILayout.HelpBox("ScenePath vide — SceneReference pas synchronisée. Clique Re-Sync.", MessageType.Error);
                if (GUILayout.Button("Re-Sync"))
                {
                    screen.m_scene.Sync();
                    EditorUtility.SetDirty(screen);
                    AssetDatabase.SaveAssets();
                }
            }
            else if (!string.IsNullOrEmpty(syncedPath))
            {
                EditorGUILayout.LabelField($"  Path : {syncedPath}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(5);

            // --- Config du screen ---
            EditorGUILayout.LabelField("Configuration", EditorStyles.miniBoldLabel);

            EditorGUI.BeginChangeCheck();

            bool isOverlay = EditorGUILayout.Toggle("Is Overlay", screen.m_isOverlay);
            bool blockPause = EditorGUILayout.Toggle("Block Pause", screen.m_blockPause);
            bool loadOnStart = EditorGUILayout.Toggle("Load On Start", screen.m_loadOnStart);
            var showTrigger = (UITrigger)EditorGUILayout.EnumPopup("Show Trigger", screen.m_showTrigger);
            var hideTrigger = (UIHideTrigger)EditorGUILayout.EnumPopup("Hide Trigger", screen.m_hideTrigger);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(screen, "Edit UIScreen");

                bool wasLoadOnStart = screen.m_loadOnStart;
                screen.m_isOverlay = isOverlay;
                screen.m_blockPause = blockPause;
                screen.m_loadOnStart = loadOnStart;
                screen.m_showTrigger = showTrigger;
                screen.m_hideTrigger = hideTrigger;

                EditorUtility.SetDirty(screen);

                if (screen.m_loadOnStart && !wasLoadOnStart)
                {
                    foreach (var s in _screens.ToList())
                    {
                        if (s != screen && s.m_loadOnStart)
                        {
                            Undo.RecordObject(s, "Edit UIScreen");
                            s.m_loadOnStart = false;
                            EditorUtility.SetDirty(s);
                        }
                    }
                }

                AssetDatabase.SaveAssets();
            }

            EditorGUI.indentLevel--;

            // --- Boutons ---
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Boutons", EditorStyles.miniBoldLabel);

            var buttons = GetButtonsInScreen(screen);
            if (buttons.Count == 0)
            {
                EditorGUILayout.LabelField("  Aucun bouton détecté (charge la scène via Set).", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var btn in buttons)
                    DrawButtonSetup(btn, screen);

                EditorGUILayout.Space(3);
                if (GUILayout.Button("⟳ Auto-setup Navigation", GUILayout.Height(22)))
                    SetupNavigation(buttons);
            }
        }

        private void DrawButtonSetup(UnityEngine.UI.Button btn, UIScreen screen)
        {
            if (btn == null) return;

            var action = btn.GetComponent<UIButtonAction>();
            var feedback = btn.GetComponent<ClickButtonFeedback>();
            var sfx = btn.GetComponent<ButtonSFX>();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"  {btn.gameObject.name}", EditorStyles.miniBoldLabel);

            if (action == null)
            {
                if (GUILayout.Button("+ Ajouter Action", GUILayout.Height(18)))
                {
                    Undo.AddComponent<UIButtonAction>(btn.gameObject);
                    EditorUtility.SetDirty(btn.gameObject);
                }
            }
            else
            {
                var actionSo = new SerializedObject(action);
                actionSo.Update();

                EditorGUILayout.PropertyField(actionSo.FindProperty("m_actionType"));

                var actionType = (ButtonActionType)actionSo.FindProperty("m_actionType").enumValueIndex;
                if (actionType == ButtonActionType.ShowScreen
                    || actionType == ButtonActionType.HideScreen
                    || actionType == ButtonActionType.StartGame
                    || actionType == ButtonActionType.RestartGame
                    || actionType == ButtonActionType.MainMenu)
                {
                    var screenNames = _screens.ConvertAll(s => s.name).ToArray();
                    int currentIndex = _screens.IndexOf(action.TargetScreen);
                    if (currentIndex < 0) currentIndex = 0;
                    int newIndex = EditorGUILayout.Popup("Target Screen", currentIndex, screenNames);
                    if (newIndex != currentIndex && _screens.Count > 0)
                    {
                        Undo.RecordObject(action, "Change Target Screen");
                        action.TargetScreen = _screens[newIndex];
                        EditorUtility.SetDirty(action);
                    }
                }
                else if (actionType == ButtonActionType.HideCurrent)
                {
                    action.CurrentScreen = screen;
                    EditorUtility.SetDirty(action);
                    EditorGUILayout.LabelField($"  → Cache : {screen.name}", EditorStyles.miniLabel);
                }

                if (actionSo.ApplyModifiedProperties())
                    EditorUtility.SetDirty(action);

                if (GUILayout.Button("✕ Retirer action", GUILayout.Height(18)))
                {
                    Undo.DestroyObjectImmediate(action);
                    EditorUtility.SetDirty(btn.gameObject);
                }
            }

            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(feedback != null);
            if (GUILayout.Button(feedback != null ? "✓ Feedback" : "+ Feedback", GUILayout.Height(18)))
            {
                Undo.AddComponent<ClickButtonFeedback>(btn.gameObject);
                EditorUtility.SetDirty(btn.gameObject);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(sfx != null);
            if (GUILayout.Button(sfx != null ? "✓ SFX" : "+ SFX", GUILayout.Height(18)))
            {
                Undo.AddComponent<ButtonSFX>(btn.gameObject);
                EditorUtility.SetDirty(btn.gameObject);
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            if (sfx != null)
            {
                var sfxSo = new SerializedObject(sfx);
                sfxSo.Update();
                EditorGUILayout.PropertyField(sfxSo.FindProperty("m_audioSource"));
                EditorGUILayout.PropertyField(sfxSo.FindProperty("m_hoverSound"));
                if (sfxSo.ApplyModifiedProperties())
                    EditorUtility.SetDirty(sfx);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private void DrawLoadBar()
        {
            EditorGUILayout.LabelField("Charger un Screen", EditorStyles.boldLabel);

            if (_screens.Count == 0) return;

            if (_selectedLoadIndex >= _screens.Count)
                _selectedLoadIndex = 0;

            var screenNames = _screens.ConvertAll(s => s.name).ToArray();
            _selectedLoadIndex = EditorGUILayout.Popup("Screen", _selectedLoadIndex, screenNames);

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            if (GUILayout.Button("Loader dans l'éditeur", GUILayout.Height(30)))
                LoadInEditor(_screens[_selectedLoadIndex]);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            if (GUILayout.Button("Show In Game", GUILayout.Height(30)))
            {
                var manager = FindFirstObjectByType<UIManager>();
                if (manager != null) manager.Show(_screens[_selectedLoadIndex]);
            }
            EditorGUI.EndDisabledGroup();
        }

        private static void DrawSeparator()
        {
            EditorGUILayout.Space(4);
            Rect r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(4);
        }

        #endregion


        #region Operations

        private void SyncAll()
        {
            if (_config == null) return;

            int synced = 0;

            if (_config.m_managerScene?.m_sceneAsset != null)
            {
                _config.m_managerScene.Sync();
                RegisterSceneToBuildSettings(_config.m_managerScene.ScenePath);
                EditorUtility.SetDirty(_config);
                synced++;
            }

            foreach (var screen in _config.m_screens)
            {
                if (screen == null || screen.m_scene?.m_sceneAsset == null) continue;
                screen.m_scene.Sync();
                RegisterSceneToBuildSettings(screen.m_scene.ScenePath);
                EditorUtility.SetDirty(screen);
                synced++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[UI MANAGER]: Sync All → {synced} scène(s) synchronisée(s) + Build Settings à jour");
        }

        private void AssignScene(ref SceneReference sceneRef, SceneAsset asset, Object owner)
        {
            if (sceneRef == null) sceneRef = new SceneReference();

            var oldPath = sceneRef.ScenePath;
            if (!string.IsNullOrEmpty(oldPath)) UnregisterSceneFromBuildSettings(oldPath);

            sceneRef.m_sceneAsset = asset;
            sceneRef.Sync();

            EditorUtility.SetDirty(owner);
            AssetDatabase.SaveAssets();

            var newPath = sceneRef.ScenePath;
            if (!string.IsNullOrEmpty(newPath)) RegisterSceneToBuildSettings(newPath);
        }

        private List<UnityEngine.UI.Button> GetButtonsInScreen(UIScreen screen)
        {
            var result = new List<UnityEngine.UI.Button>();
            var path = screen.m_scene?.ScenePath;
            if (string.IsNullOrEmpty(path)) return result;

            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            if (!scene.isLoaded) return result;

            foreach (var root in scene.GetRootGameObjects())
                result.AddRange(root.GetComponentsInChildren<UnityEngine.UI.Button>(true));

            return result;
        }

        private void SetupNavigation(List<UnityEngine.UI.Button> buttons)
        {
            if (buttons.Count == 0) return;

            for (int i = 0; i < buttons.Count; i++)
            {
                var nav = new UnityEngine.UI.Navigation();
                nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                nav.selectOnUp = i > 0 ? buttons[i - 1] : buttons[buttons.Count - 1];
                nav.selectOnDown = i < buttons.Count - 1 ? buttons[i + 1] : buttons[0];
                buttons[i].navigation = nav;
                EditorUtility.SetDirty(buttons[i]);
            }

            Debug.Log($"[UI MANAGER]: Navigation configurée pour {buttons.Count} boutons");
        }

        private void LoadInEditor(UIScreen screen)
        {
            var path = screen.m_scene?.ScenePath;
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning($"[UI MANAGER]: '{screen.name}' n'a aucune scène");
                return;
            }

            EditorSceneManager.SaveOpenScenes();

            var managerAsset = _config?.m_managerScene?.m_sceneAsset;
            if (managerAsset != null)
            {
                var managerPath = AssetDatabase.GetAssetPath(managerAsset);
                EditorSceneManager.OpenScene(managerPath, OpenSceneMode.Single);
            }
            else
            {
                for (int i = EditorSceneManager.sceneCount - 1; i >= 0; i--)
                    EditorSceneManager.CloseScene(EditorSceneManager.GetSceneAt(i), true);
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Debug.Log($"[UI MANAGER]: '{screen.name}' chargé dans l'éditeur");
        }

        private void CreateScreen(string screenName)
        {
            if (!AssetDatabase.IsValidFolder(_screensDatabase))
                AssetDatabase.CreateFolder("Assets/_/Database", "UIScreens");

            var screen = ScriptableObject.CreateInstance<UIScreen>();
            AssetDatabase.CreateAsset(screen, $"{_screensDatabase}/{screenName}.asset");
            AssetDatabase.SaveAssets();

            if (_config != null)
            {
                _config.m_screens.Add(screen);
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[UI MANAGER]: Screen '{screenName}' créé");
        }

        private void DeleteScreen(UIScreen screen)
        {
            var scenePath = screen.m_scene?.ScenePath;
            if (!string.IsNullOrEmpty(scenePath)) UnregisterSceneFromBuildSettings(scenePath);

            var screenPath = AssetDatabase.GetAssetPath(screen);

            _config.m_screens.Remove(screen);
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();

            AssetDatabase.DeleteAsset(screenPath);

            _selectedScreen = null;
            RefreshScreens();
        }

        private void RefreshScreens()
        {
            _screens.Clear();
            if (_config == null) return;
            foreach (var screen in _config.m_screens)
                if (screen != null) _screens.Add(screen);
        }

        private void RegisterSceneToBuildSettings(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var buildScenes = EditorBuildSettings.scenes.ToList();
            if (buildScenes.Any(s => s.path == path)) return;
            buildScenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log($"[UI MANAGER]: '{path}' ajoutée aux Build Settings");
        }

        private void UnregisterSceneFromBuildSettings(string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            // Ne pas retirer si une autre référence (screen ou manager) pointe encore dessus
            bool stillUsed = _config != null &&
                (_config.m_managerScene?.ScenePath == path ||
                 _config.m_screens.Exists(s => s != null && s.m_scene?.ScenePath == path));
            if (stillUsed) return;

            var buildScenes = EditorBuildSettings.scenes.ToList();
            int removed = buildScenes.RemoveAll(s => s.path == path);
            if (removed > 0)
            {
                EditorBuildSettings.scenes = buildScenes.ToArray();
                Debug.Log($"[UI MANAGER]: '{path}' retirée des Build Settings");
            }
        }

        private void LoadConfig()
        {
            _config = Resources.Load<UIManagerConfig>("UIManagerConfig");
        }

        private void CreateConfig()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_/Database/Settings"))
                AssetDatabase.CreateFolder("Assets/_/Database", "Settings");
            if (!AssetDatabase.IsValidFolder(_configFolder))
                AssetDatabase.CreateFolder("Assets/_/Database/Settings", "Resources");

            _config = ScriptableObject.CreateInstance<UIManagerConfig>();
            AssetDatabase.CreateAsset(_config, _configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #endregion


        #region Private

        private Vector2 _scroll;
        private UIManagerConfig _config;
        private UIScreen _selectedScreen;
        private List<UIScreen> _screens = new();
        private string _newScreenName = "";
        private int _selectedLoadIndex = 0;

        #endregion

    }
}
